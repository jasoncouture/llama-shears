using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class ExtensionMethodOnThisAnalyzerTests
{
    [Test]
    public async Task ThisExtensionMethodCallReportsLS0006AsWarning()
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
    public async Task DescriptorIsConfigurable()
    {
        var analyzer = new ExtensionMethodOnThisAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        await Assert.That(descriptor.CustomTags).DoesNotContain("NotConfigurable");
    }

    [Test]
    public async Task ThisInstanceMethodCallDoesNotReport()
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
    public async Task ExtensionCallOnOtherInstanceDoesNotReport()
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
    public async Task BareThisDoesNotReport()
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
