using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class NoPublicOrInternalFieldsAnalyzerTests
{
    [Test]
    public async Task PublicFieldReportsLS0002()
    {
        const string source = "public class Foo { public int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.NoPublicOrInternalFields);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("public");
    }

    [Test]
    public async Task InternalFieldReportsLS0002()
    {
        const string source = "public class Foo { internal int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("internal");
    }

    [Test]
    public async Task ProtectedFieldReportsLS0002()
    {
        const string source = "public class Foo { protected int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("protected");
    }

    [Test]
    public async Task ProtectedInternalFieldReportsLS0002()
    {
        const string source = "public class Foo { protected internal int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task PrivateProtectedFieldReportsLS0002()
    {
        const string source = "public class Foo { private protected int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task ExplicitPrivateFieldDoesNotReport()
    {
        const string source = "public class Foo { private int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task DefaultUnmodifiedFieldIsPrivateAndDoesNotReport()
    {
        const string source = "public class Foo { int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task PublicConstFieldDoesNotReport()
    {
        const string source = "public class Foo { public const int X = 1; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task MultiDeclaratorFieldReportsForEachVariable()
    {
        const string source = "public class Foo { public int x, y; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(2);
    }
}
