using LlamaShears.Core.Abstractions.Provider;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.Tokenizers;

namespace LlamaShears.Provider.Onnx.Embeddings;

// Single-instance per (modelPath, vocabPath). Heavy state (the
// InferenceSession + tokenizer) is loaded once and reused; Run is
// thread-safe per ONNX Runtime documentation, the BertTokenizer is
// immutable after construction.
public sealed class OnnxEmbeddingModel : IEmbeddingModel, IDisposable
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private readonly OnnxModelOptions _options;
    private readonly bool _emitsTokenTypeIds;

    public OnnxEmbeddingModel(OnnxModelOptions options, string modelFullPath, string vocabFullPath)
    {
        _options = options;
        _session = new InferenceSession(modelFullPath);
        var bertOptions = new BertOptions
        {
            LowerCaseBeforeTokenization = options.LowerCase,
            ApplyBasicTokenization = true,
        };
        _tokenizer = BertTokenizer.Create(vocabFullPath, bertOptions);
        _emitsTokenTypeIds = _session.InputMetadata.ContainsKey(TokenTypeIdsName);
    }

    public async ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();
        // Tokenization and tensor work is CPU-bound and fast for a
        // single sentence — keep it on the calling thread instead of
        // bouncing through Task.Run.
        return await ValueTask.FromResult(EmbedCore(text)).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0)
        {
            return [];
        }
        // First-cut implementation: one inference per text. Real batching
        // requires padding to the longest sequence in the batch; until a
        // hot path actually demands it, the loop keeps the code simple.
        var results = new ReadOnlyMemory<float>[texts.Count];
        for (var i = 0; i < texts.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results[i] = EmbedCore(texts[i]);
        }
        return await ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results).ConfigureAwait(false);
    }

    public void Dispose() => _session.Dispose();

    private const string InputIdsName = "input_ids";
    private const string AttentionMaskName = "attention_mask";
    private const string TokenTypeIdsName = "token_type_ids";
    private const string OutputName = "last_hidden_state";

    private float[] EmbedCore(string text)
    {
        var ids = _tokenizer.EncodeToIds(
            text,
            maxTokenCount: _options.MaxSequenceLength,
            addSpecialTokens: true,
            normalizedText: out _,
            charsConsumed: out _);
        var seqLen = ids.Count;

        var inputIds = new long[seqLen];
        var attentionMask = new long[seqLen];
        for (var i = 0; i < seqLen; i++)
        {
            inputIds[i] = ids[i];
            attentionMask[i] = 1L;
        }
        var tokenTypeIds = _emitsTokenTypeIds ? new long[seqLen] : null;

        long[] shape = [1, seqLen];
        using var inputIdsTensor = OrtValue.CreateTensorValueFromMemory(inputIds, shape);
        using var attentionMaskTensor = OrtValue.CreateTensorValueFromMemory(attentionMask, shape);
        OrtValue? tokenTypeIdsTensor = null;
        try
        {
            if (tokenTypeIds is not null)
            {
                tokenTypeIdsTensor = OrtValue.CreateTensorValueFromMemory(tokenTypeIds, shape);
            }

            string[] inputNames;
            OrtValue[] inputValues;
            if (tokenTypeIdsTensor is not null)
            {
                inputNames = [InputIdsName, AttentionMaskName, TokenTypeIdsName];
                inputValues = [inputIdsTensor, attentionMaskTensor, tokenTypeIdsTensor];
            }
            else
            {
                inputNames = [InputIdsName, AttentionMaskName];
                inputValues = [inputIdsTensor, attentionMaskTensor];
            }

            using var runOptions = new RunOptions();
            using var outputs = _session.Run(runOptions, inputNames, inputValues, [OutputName]);
            var output = outputs[0];
            var hidden = output.GetTensorDataAsSpan<float>();
            // Shape: [batch=1, seqLen, hiddenDim]. hiddenDim is the
            // model's pooling dimension (384 for minilm-l6-v2).
            var hiddenDim = hidden.Length / seqLen;
            var pooled = _options.Pooling switch
            {
                OnnxPoolingStrategy.Mean => MeanPool(hidden, attentionMask, seqLen, hiddenDim),
                OnnxPoolingStrategy.Cls => ClsPool(hidden, hiddenDim),
                _ => throw new InvalidOperationException($"Unsupported pooling strategy '{_options.Pooling}'."),
            };
            if (_options.Normalize)
            {
                L2Normalize(pooled);
            }
            return pooled;
        }
        finally
        {
            tokenTypeIdsTensor?.Dispose();
        }
    }

    private static float[] MeanPool(ReadOnlySpan<float> hidden, long[] attentionMask, int seqLen, int hiddenDim)
    {
        var pooled = new float[hiddenDim];
        long maskSum = 0;
        for (var i = 0; i < seqLen; i++)
        {
            if (attentionMask[i] == 0)
            {
                continue;
            }
            maskSum++;
            var rowStart = i * hiddenDim;
            for (var j = 0; j < hiddenDim; j++)
            {
                pooled[j] += hidden[rowStart + j];
            }
        }
        if (maskSum > 0)
        {
            var inv = 1f / maskSum;
            for (var j = 0; j < hiddenDim; j++)
            {
                pooled[j] *= inv;
            }
        }
        return pooled;
    }

    private static float[] ClsPool(ReadOnlySpan<float> hidden, int hiddenDim)
    {
        var pooled = new float[hiddenDim];
        hidden[..hiddenDim].CopyTo(pooled);
        return pooled;
    }

    private static void L2Normalize(Span<float> vector)
    {
        double sumSquares = 0;
        for (var i = 0; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }
        if (sumSquares <= 0)
        {
            return;
        }
        var inv = (float)(1.0 / Math.Sqrt(sumSquares));
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] *= inv;
        }
    }
}
