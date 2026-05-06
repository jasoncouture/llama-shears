using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class FieldMustStartWithUnderscoreAnalyzerTests
{
    [Test]
    public async Task FieldWithoutUnderscoreReportsLS0003()
    {
        const string source = "public class Foo { private int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.FieldMustStartWithUnderscore);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("'x'");
    }

    [Test]
    public async Task FieldStartingWithUnderscoreDoesNotReport()
    {
        const string source = "public class Foo { private int _x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ConstFieldIsExempt()
    {
        const string source = "public class Foo { public const int Default = 1; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task StaticReadonlyFieldMustStartWithUnderscore()
    {
        const string source = "public class Foo { private static readonly int Default = 1; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task MultiDeclaratorFieldReportsPerViolatorOnly()
    {
        const string source = "public class Foo { private int _x, y; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("'y'");
    }
}
