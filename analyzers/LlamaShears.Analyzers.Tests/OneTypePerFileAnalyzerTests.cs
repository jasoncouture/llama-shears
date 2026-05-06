using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class OneTypePerFileAnalyzerTests
{
    [Test]
    public async Task Single_type_does_not_report()
    {
        const string source = "namespace N; public class Foo { }";

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new OneTypePerFileAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task Two_top_level_classes_report_on_second()
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
    public async Task Three_top_level_types_report_on_extras()
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
    public async Task Nested_types_do_not_report()
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
    public async Task Top_level_delegate_counts_as_a_type()
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
    public async Task Types_in_separate_namespaces_in_one_file_still_report()
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
