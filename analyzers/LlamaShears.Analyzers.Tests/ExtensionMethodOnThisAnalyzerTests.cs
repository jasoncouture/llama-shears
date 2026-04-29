using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class ExtensionMethodOnThisAnalyzerTests
{
    [Test]
    public async Task This_extension_method_call_reports_LS0006_as_warning()
    {
        const string source =
            """
            public class Foo { }

            public static class FooExtensions
            {
                public static int Score(this Foo foo) => 42;
            }

            public class Bar : Foo
            {
                public int Run() => this.Score();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExtensionMethodOnThisAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ExtensionMethodOnThis);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Score");
    }

    [Test]
    public async Task Descriptor_is_configurable()
    {
        var analyzer = new ExtensionMethodOnThisAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        await Assert.That(descriptor.CustomTags).DoesNotContain("NotConfigurable");
    }

    [Test]
    public async Task This_instance_method_call_does_not_report()
    {
        const string source =
            """
            public class Foo
            {
                private void Helper() { }
                public void Run() => this.Helper();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExtensionMethodOnThisAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Extension_call_on_other_instance_does_not_report()
    {
        const string source =
            """
            public class Foo { }

            public static class FooExtensions
            {
                public static int Score(this Foo foo) => 42;
            }

            public class Caller
            {
                public int Run(Foo foo) => foo.Score();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExtensionMethodOnThisAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Bare_this_does_not_report()
    {
        const string source =
            """
            public class Foo
            {
                public Foo Get() => this;
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExtensionMethodOnThisAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }
}
