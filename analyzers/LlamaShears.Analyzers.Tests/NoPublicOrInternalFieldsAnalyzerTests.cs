using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class NoPublicOrInternalFieldsAnalyzerTests
{
    [Test]
    public async Task Public_field_reports_LS0002()
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
    public async Task Internal_field_reports_LS0002()
    {
        const string source = "public class Foo { internal int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("internal");
    }

    [Test]
    public async Task Protected_field_reports_LS0002()
    {
        const string source = "public class Foo { protected int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("protected");
    }

    [Test]
    public async Task Protected_internal_field_reports_LS0002()
    {
        const string source = "public class Foo { protected internal int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task Private_protected_field_reports_LS0002()
    {
        const string source = "public class Foo { private protected int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task Explicit_private_field_does_not_report()
    {
        const string source = "public class Foo { private int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Default_unmodified_field_is_private_and_does_not_report()
    {
        const string source = "public class Foo { int x; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Public_const_field_does_not_report()
    {
        const string source = "public class Foo { public const int X = 1; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Multi_declarator_field_reports_for_each_variable()
    {
        const string source = "public class Foo { public int x, y; }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoPublicOrInternalFieldsAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(2);
    }
}
