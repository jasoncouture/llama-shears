using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class PrimaryConstructorOnNonRecordAnalyzerTests
{
    [Test]
    public async Task ClassWithPrimaryConstructorReportsLS0001AsError()
    {
        const string source = "public class Foo(int x) { public int X => x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        var diagnostic = diagnostics[0];
        await Assert.That(diagnostic.Id).IsEqualTo(DiagnosticIds.PrimaryConstructorOnNonRecord);
        await Assert.That(diagnostic.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostic.GetMessage()).Contains("Foo");
    }

    [Test]
    public async Task StructWithPrimaryConstructorReportsLS0001()
    {
        const string source = "public struct Bar(int x) { public int X => x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.PrimaryConstructorOnNonRecord);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Bar");
    }

    [Test]
    public async Task RecordClassWithPrimaryConstructorDoesNotReport()
    {
        const string source = "public record Foo(int X);";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task RecordStructWithPrimaryConstructorDoesNotReport()
    {
        const string source = "public record struct Bar(int X);";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ClassWithoutPrimaryConstructorDoesNotReport()
    {
        const string source =
            """
            public class Foo
            {
                private readonly int x;

                public Foo(int x)
                {
                    this.x = x;
                }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }
}
