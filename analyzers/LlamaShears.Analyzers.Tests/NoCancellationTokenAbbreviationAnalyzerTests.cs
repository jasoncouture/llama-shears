using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class NoCancellationTokenAbbreviationAnalyzerTests
{
    [Test]
    public async Task ParameterNamedCtReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar(CancellationToken ct) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.NoCancellationTokenAbbreviation);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("'ct'");
    }

    [Test]
    public async Task ParameterNamedCtsReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar(CancellationTokenSource cts) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.NoCancellationTokenAbbreviation);
        await Assert.That(diagnostics[0].GetMessage()).Contains("'cts'");
    }

    [Test]
    public async Task PropertyNamedCtsReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public CancellationTokenSource Cts { get; } = new();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("'Cts'");
    }

    [Test]
    public async Task PropertyNamedCtReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public CancellationToken Ct { get; }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task FieldNamedUnderscoreCtsReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                private CancellationTokenSource _cts = new();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("'_cts'");
    }

    [Test]
    public async Task ErrorMessageContainsTheUserApprovedRebuke()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar(CancellationToken ct) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics[0].GetMessage())
            .Contains("KNOCK IT THE FUCK OFF AND NAME IT CORRECTLY");
    }

    [Test]
    public async Task FieldNamedUnderscoreCtReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                private CancellationToken _ct;
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].GetMessage()).Contains("'_ct'");
    }

    [Test]
    public async Task LocalVariableNamedCtReportsLS0007()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar()
                {
                    CancellationToken ct = default;
                }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task LambdaParameterNamedCtReportsLS0007()
    {
        const string source = """
            using System;
            using System.Threading;
            public class Foo
            {
                public Action<CancellationToken> Bar() => ct => { };
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task ParameterNamedCancellationTokenDoesNotReport()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar(CancellationToken cancellationToken) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task UnrelatedNameContainingTheLettersCtDoesNotReport()
    {
        // We strip leading underscores and compare the remainder to "ct"
        // or "cts" case-insensitively. Identifiers that merely contain
        // those letters (e.g. "context", "connect", "contents") are
        // unaffected.
        const string source = """
            public class Foo
            {
                public void Bar(int context, int connect, int contents) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task UppercaseCtIsBanned()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar(CancellationToken CT) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task PascalCaseCtIsBanned()
    {
        const string source = """
            using System.Threading;
            public class Foo
            {
                public void Bar(CancellationToken Ct) { }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task ForeachVariableNamedCtReportsLS0007()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            public class Foo
            {
                public void Bar(IEnumerable<CancellationToken> tokens)
                {
                    foreach (var ct in tokens) { }
                }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new NoCancellationTokenAbbreviationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
    }

    [Test]
    public async Task RuleIsNotConfigurable()
    {
        var descriptor = Descriptors.NoCancellationTokenAbbreviation;

        await Assert.That(descriptor.CustomTags).Contains(WellKnownDiagnosticTags.NotConfigurable);
        await Assert.That(descriptor.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
    }
}
