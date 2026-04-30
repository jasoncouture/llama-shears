using LlamaShears.Analyzers.Diagnostics;

namespace LlamaShears.Analyzers.Tests;

public sealed class SuppressCs1591Tests
{
    [Test]
    public async Task SuppressionDescriptorTargetsCs1591()
    {
        var suppressor = new SuppressCs1591();

        await Assert.That(suppressor.SupportedSuppressions).HasSingleItem();
        var descriptor = suppressor.SupportedSuppressions[0];
        await Assert.That(descriptor.Id).IsEqualTo(DiagnosticIds.SuppressCs1591);
        await Assert.That(descriptor.SuppressedDiagnosticId).IsEqualTo("CS1591");
    }
}
