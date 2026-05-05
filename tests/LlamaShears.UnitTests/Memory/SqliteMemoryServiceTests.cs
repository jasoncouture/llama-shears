namespace LlamaShears.UnitTests.Memory;

public sealed class SqliteMemoryServiceTests
{
    [Test]
    public async Task StoreWritesFileUnderDateFolder()
    {
        using var h = MemoryTestHarness.Create();

        var memory = await h.Service.StoreAsync(h.AgentId, "first memory", CancellationToken.None);

        await Assert.That(memory.RelativePath).StartsWith("memory/");
        await Assert.That(memory.RelativePath).EndsWith(".md");
        await Assert.That(File.Exists(h.PathOf(memory.RelativePath.Replace('/', Path.DirectorySeparatorChar)))).IsTrue();
    }

    [Test]
    public async Task StoreCreatesIndexDbUnderSystemFolder()
    {
        using var h = MemoryTestHarness.Create();

        await h.Service.StoreAsync(h.AgentId, "anything", CancellationToken.None);

        await Assert.That(File.Exists(h.PathOf("system", ".memory.db"))).IsTrue();
    }

    [Test]
    public async Task StoredMemoryIsFoundBySearch()
    {
        using var h = MemoryTestHarness.Create();
        var memory = await h.Service.StoreAsync(h.AgentId, "the quick brown fox", CancellationToken.None);

        var hits = await h.Service.SearchAsync(h.AgentId, "the quick brown fox", limit: 5, minScore: 0.0, CancellationToken.None);

        await Assert.That(hits.Count).IsEqualTo(1);
        await Assert.That(hits[0].RelativePath).IsEqualTo(memory.RelativePath);
        await Assert.That(hits[0].Score).IsGreaterThanOrEqualTo(0.99);
    }

    [Test]
    public async Task SearchHonorsMinScore()
    {
        using var h = MemoryTestHarness.Create();
        await h.Service.StoreAsync(h.AgentId, "alpha beta gamma", CancellationToken.None);

        var hits = await h.Service.SearchAsync(h.AgentId, "zzzzz", limit: 5, minScore: 0.99, CancellationToken.None);

        await Assert.That(hits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SearchOnEmptyWorkspaceReturnsEmpty()
    {
        using var h = MemoryTestHarness.Create();

        var hits = await h.Service.SearchAsync(h.AgentId, "anything", limit: 5, minScore: 0.0, CancellationToken.None);

        await Assert.That(hits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ReconcilePicksUpUnindexedFiles()
    {
        using var h = MemoryTestHarness.Create();
        var dir = h.PathOf("memory", "2026-01-01");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "1.md"), "out-of-band write");

        var summary = await h.Service.ReconcileAsync(h.AgentId, force: false, CancellationToken.None);

        await Assert.That(summary.Added).IsEqualTo(1);
        await Assert.That(summary.Updated).IsEqualTo(0);
        await Assert.That(summary.Removed).IsEqualTo(0);
        await Assert.That(summary.Total).IsEqualTo(1);
    }

    [Test]
    public async Task ReconcileReindexesChangedFiles()
    {
        using var h = MemoryTestHarness.Create();
        var memory = await h.Service.StoreAsync(h.AgentId, "original", CancellationToken.None);
        var full = h.PathOf(memory.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        await File.WriteAllTextAsync(full, "edited out of band");

        var summary = await h.Service.ReconcileAsync(h.AgentId, force: false, CancellationToken.None);

        await Assert.That(summary.Updated).IsEqualTo(1);
        await Assert.That(summary.Added).IsEqualTo(0);
        await Assert.That(summary.Removed).IsEqualTo(0);
        await Assert.That(summary.Total).IsEqualTo(1);
    }

    [Test]
    public async Task ReconcileRemovesOrphanedIndexEntries()
    {
        using var h = MemoryTestHarness.Create();
        var memory = await h.Service.StoreAsync(h.AgentId, "soon to vanish", CancellationToken.None);
        var full = h.PathOf(memory.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Delete(full);

        var summary = await h.Service.ReconcileAsync(h.AgentId, force: false, CancellationToken.None);

        await Assert.That(summary.Removed).IsEqualTo(1);
        await Assert.That(summary.Added).IsEqualTo(0);
        await Assert.That(summary.Updated).IsEqualTo(0);
        await Assert.That(summary.Total).IsEqualTo(0);
    }

    [Test]
    public async Task ForceReconcileReembedsUnchangedFiles()
    {
        using var h = MemoryTestHarness.Create();
        await h.Service.StoreAsync(h.AgentId, "stable content", CancellationToken.None);

        var summary = await h.Service.ReconcileAsync(h.AgentId, force: true, CancellationToken.None);

        // The file's hash hasn't changed, so without force the count is
        // zero across the board. Force flips the unchanged file into the
        // updated bucket.
        await Assert.That(summary.Updated).IsEqualTo(1);
        await Assert.That(summary.Added).IsEqualTo(0);
        await Assert.That(summary.Removed).IsEqualTo(0);
        await Assert.That(summary.Total).IsEqualTo(1);
    }

    [Test]
    public async Task SearchSkipsHitsWithMissingBackingFile()
    {
        using var h = MemoryTestHarness.Create();
        var memory = await h.Service.StoreAsync(h.AgentId, "ghost", CancellationToken.None);
        File.Delete(h.PathOf(memory.RelativePath.Replace('/', Path.DirectorySeparatorChar)));

        var hits = await h.Service.SearchAsync(h.AgentId, "ghost", limit: 5, minScore: 0.0, CancellationToken.None);

        await Assert.That(hits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ReconcileRebuildsIndexWhenEmbedderDimensionChanges()
    {
        // Index a memory at one dimension, then swap the embedder to a
        // different dimension and reconcile. SqliteVec rejects the new
        // upserts as a NOT NULL constraint failure on memories.vector;
        // the service must catch that, blow away the index db, and
        // rebuild from disk. force:true mirrors the host-side
        // ForceOnStartup behaviour that triggers upserts even when file
        // hashes are unchanged.
        using var h = MemoryTestHarness.CreateWithVariableDimension(initialDimensions: 16);
        var memory = await h.Service.StoreAsync(h.AgentId, "stable content", CancellationToken.None);

        h.VariableDim!.Dimensions = 32;

        var summary = await h.Service.ReconcileAsync(h.AgentId, force: true, CancellationToken.None);

        // After the rebuild the file is the only source of truth: it
        // re-appears as Added (the index started empty after reset) and
        // Total = 1.
        await Assert.That(summary.Added).IsEqualTo(1);
        await Assert.That(summary.Total).IsEqualTo(1);

        // And the rebuilt index is queryable at the new dimension.
        var hits = await h.Service.SearchAsync(h.AgentId, "stable content", limit: 5, minScore: 0.0, CancellationToken.None);
        await Assert.That(hits.Count).IsEqualTo(1);
        await Assert.That(hits[0].RelativePath).IsEqualTo(memory.RelativePath);
    }

    [Test]
    public async Task SecondStoreInSameSecondGetsDistinctPath()
    {
        using var h = MemoryTestHarness.Create();

        var first = await h.Service.StoreAsync(h.AgentId, "one", CancellationToken.None);
        var second = await h.Service.StoreAsync(h.AgentId, "two", CancellationToken.None);

        await Assert.That(second.RelativePath).IsNotEqualTo(first.RelativePath);
        await Assert.That(File.Exists(h.PathOf(first.RelativePath.Replace('/', Path.DirectorySeparatorChar)))).IsTrue();
        await Assert.That(File.Exists(h.PathOf(second.RelativePath.Replace('/', Path.DirectorySeparatorChar)))).IsTrue();
    }
}
