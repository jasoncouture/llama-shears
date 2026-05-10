namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent memory-subsystem options. Read by the memory searcher
/// when a caller does not pass explicit per-call overrides.
/// </summary>
/// <param name="SearchLimit">Default maximum number of hits to return from a memory search. Callers may override per call.</param>
/// <param name="SearchMinScore">Default minimum cosine similarity (0.0–1.0); hits scoring below are dropped. Callers may override per call. The default of 0.30 sits in the empirical gap between "noise" (under ~0.10) and "relevant" (~0.40–0.60) for embeddinggemma-class encoders with task prefixes.</param>
public sealed record AgentMemoryConfig(int SearchLimit = 5, double SearchMinScore = 0.30);
