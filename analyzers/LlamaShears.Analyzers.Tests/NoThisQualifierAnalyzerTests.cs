using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class NoThisQualifierAnalyzerTests
{
    [Test]
    public async Task ThisFieldAccessReportsLS0004()
    {
        const string source =
            """
            public class Foo
            {
                private int _x;
                public int Read() => this._x;
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoThisQualifierAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.NoThisQualifier);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ThisPropertyAccessReportsLS0004()
    {
        const string source =
            """
            public class Foo
            {
                public int Bar { get; set; }
                public int Read() => this.Bar;
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoThisQualifierAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task ThisInstanceMethodCallReportsLS0004()
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
            new NoThisQualifierAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task ThisExtensionMethodCallDoesNotReport()
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
            new NoThisQualifierAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task BareThisInReturnDoesNotReport()
    {
        const string source =
            """
            public class Foo
            {
                public Foo Get() => this;
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoThisQualifierAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task BareThisPassedAsArgumentDoesNotReport()
    {
        const string source =
            """
            public class Foo
            {
                public bool Equals(Foo other) => ReferenceEquals(this, other);
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoThisQualifierAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }
}
