---
name: ONNX embeddings provider — gotchas and conventions
description: LlamaShears.Provider.Onnx.Embeddings landed 2026-05-05; this captures the non-obvious bits about model files, paths, and runtime shape
type: project
---

`LlamaShears.Provider.Onnx.Embeddings` registers `IEmbeddingProviderFactory` with name `"ONNX"`, backed by `Microsoft.ML.OnnxRuntime` + `Microsoft.ML.Tokenizers`.

**Why it exists:** Tests the plugin contract end-to-end with a non-Ollama embedder (in-process, no daemon). all-MiniLM-L6-v2 is the first model; chat/reranker variants would be sibling sub-projects (`LlamaShears.Provider.Onnx.Chat` etc.) under the same `Provider.Onnx.*` namespace.

**How to apply:**

- **Models live under `~/.llama-shears/models/onnx`** (configurable via `OnnxEmbeddingsProviderOptions.ModelsRoot`). Per-model `ModelPath`/`VocabPath` are *relative* to that root; absolute paths and `..`-escapes are rejected by the factory. Reuse this sandbox shape for any future ONNX provider sub-project.
- **`BertTokenizer.Create` takes `vocab.txt`, not `tokenizer.json`.** This is non-obvious because HF surfaces `tokenizer.json` as the modern fast-tokenizer artifact. For all-MiniLM-L6-v2, fetch `vocab.txt` from `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt` alongside `onnx/model.onnx`.
- **Pooling + normalization are explicit.** Default is mean-pool with attention mask, then L2 normalize (CLS pooling and skipping normalization are config-toggles). Sentence-transformer-style embedders need both; if either is wrong, cosine ranks look plausible but drift.
- **`token_type_ids` is conditional.** Some ONNX exports require it as a third input, others don't. The provider auto-detects via `InferenceSession.InputMetadata` and supplies an all-zero tensor when needed.
- **No batching yet.** `EmbedAsync(IReadOnlyList<string>)` loops over single-text inferences. Real batching needs padding to the longest sequence; defer until a hot path actually demands it.
- Models are loaded lazily on first `CreateModel(...)` call and cached in the factory; the factory implements `IDisposable` so DI shutdown disposes every cached `InferenceSession`.