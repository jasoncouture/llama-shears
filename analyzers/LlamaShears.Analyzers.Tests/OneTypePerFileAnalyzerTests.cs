using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class OneTypePerFileAnalyzerTests
{
    [Test]
    public async Task SingleTypeDoesNotReport()
    {
        const string source = "namespace N; public class Foo { }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task TwoTopLevelClassesReportOnSecond()
    {
        const string source =
            """
            namespace N;

            public class Foo { }
            public class Bar { }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.OneTypePerFile);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("Bar");
    }

    [Test]
    public async Task ThreeTopLevelTypesReportOnExtras()
    {
        const string source =
            """
            namespace N;

            public class Foo { }
            public interface IBar { }
            public enum Baz { A }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).Count().IsEqualTo(2);
    }

    [Test]
    public async Task NestedTypesDoNotReport()
    {
        const string source =
            """
            namespace N;

            public class Outer
            {
                public class Inner { }
                public enum Kind { A }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task TopLevelDelegateCountsAsAType()
    {
        const string source =
            """
            namespace N;

            public class Foo { }
            public delegate void Handler();
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("Handler");
    }

    [Test]
    public async Task TypesInSeparateNamespacesInOneFileStillReport()
    {
        const string source =
            """
            namespace A
            {
                public class Foo { }
            }

            namespace B
            {
                public class Bar { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("Bar");
    }
}
