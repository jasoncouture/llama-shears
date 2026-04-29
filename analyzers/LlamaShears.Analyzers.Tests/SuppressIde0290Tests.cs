using LlamaShears.Analyzers.Diagnostics;

namespace LlamaShears.Analyzers.Tests;

public sealed class SuppressIde0290Tests
{
    [Test]
    public async Task SuppressionDescriptorTargetsIDE0290()
    {
        var suppressor = new SuppressIde0290();

        await Assert.That(suppressor.SupportedSuppressions).HasSingleItem();
        var descriptor = suppressor.SupportedSuppressions[0];
        await Assert.That(descriptor.Id).IsEqualTo(DiagnosticIds.SuppressIde0290);
        await Assert.That(descriptor.SuppressedDiagnosticId).IsEqualTo("IDE0290");
    }
}
