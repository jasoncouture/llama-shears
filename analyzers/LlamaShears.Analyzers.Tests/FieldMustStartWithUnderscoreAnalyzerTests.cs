using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class FieldMustStartWithUnderscoreAnalyzerTests
{
    [Test]
    public async Task Field_without_underscore_reports_LS0003()
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
    public async Task Field_starting_with_underscore_does_not_report()
    {
        const string source = "public class Foo { private int _x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Const_field_is_exempt()
    {
        const string source = "public class Foo { public const int Default = 1; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Static_readonly_field_must_start_with_underscore()
    {
        const string source = "public class Foo { private static readonly int Default = 1; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task Multi_declarator_field_reports_per_violator_only()
    {
        const string source = "public class Foo { private int _x, y; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new FieldMustStartWithUnderscoreAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("'y'");
    }
}
