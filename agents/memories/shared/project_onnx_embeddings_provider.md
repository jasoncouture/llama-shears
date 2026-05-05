---
name: ONNX embeddings provider — gotchas and conventions
description: LlamaShears.Provider.Onnx.Embeddings landed 2026-05-05; this captures the non-obvious bits about model files, paths, and runtime shape
type: project
---

`LlamaShears.Provider.Onnx.Embeddings` registers `IEmbeddingProviderFactory` with name `"ONNX"`, backed by `Microsoft.ML.OnnxRuntime` + `Microsoft.ML.Tokenizers`.

**Why it exists:** Tests the plugin contract end-to-end with a non-Ollama embedder (in-process, no daemon). all-MiniLM-L6-v2 is the first model; chat/reranker variants would be sibling sub-projects (`LlamaShears.Provider.Onnx.Chat` etc.) under the same `Provider.Onnx.*` namespace.

**How to apply:**

- **Models live under `<Data>/models/onnx/embeddings/<name>/`** where `<Data>` comes from `IShearsPaths.GetPath(PathKind.Data)` (default `~/.llama-shears`). The default ModelsRoot is computed at runtime from the paths provider; an explicit `OnnxEmbeddingsProviderOptions.ModelsRoot` overrides it.
- **Per-model layout is convention, not config.** Each `<name>/` directory must contain *exactly one* `*.onnx` and *exactly one* `*.txt`. The factory auto-discovers both; zero or more-than-one of either is a hard error with a clear message. The dictionary key in `Models` (which is also the agent's `model.id.model`) IS the subdirectory name.
- The `OnnxModelOptions` config carries only the per-model knobs (`MaxSequenceLength`, `LowerCase`, `Pooling`, `Normalize`, `DisplayName`, `Description`) — never paths.
- **`BertTokenizer.Create` takes `vocab.txt`, not `tokenizer.json`.** This is non-obvious because HF surfaces `tokenizer.json` as the modern fast-tokenizer artifact. For all-MiniLM-L6-v2, fetch `vocab.txt` from `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt` alongside `onnx/model.onnx`.
- **Pooling + normalization are explicit.** Default is mean-pool with attention mask, then L2 normalize (CLS pooling and skipping normalization are config-toggles). Sentence-transformer-style embedders need both; if either is wrong, cosine ranks look plausible but drift.
- **`token_type_ids` is conditional.** Some ONNX exports require it as a third input, others don't. The provider auto-detects via `InferenceSession.InputMetadata` and supplies an all-zero tensor when needed.
- **No batching yet.** `EmbedAsync(IReadOnlyList<string>)` loops over single-text inferences. Real batching needs padding to the longest sequence; defer until a hot path actually demands it.
- Models are loaded lazily on first `CreateModel(...)` call and cached in the factory; the factory implements `IDisposable` so DI shutdown disposes every cached `InferenceSession`.