using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Tests;

public sealed class ExplicitDelegateInvocationAnalyzerTests
{
    [Test]
    public async Task DirectDelegateFieldCallReportsLS0014()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                private readonly Action _handler;
                public Foo(Action handler) { _handler = handler; }
                public void Run() => _handler();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ExplicitDelegateInvocation);
        await Assert.That(diagnostics[0].DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task DirectDelegateParameterCallReportsLS0014()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                public void Run(Action handler) => handler();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ExplicitDelegateInvocation);
    }

    [Test]
    public async Task DirectDelegatePropertyCallReportsLS0014()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                public Action Handler { get; init; } = () => { };
                public void Run() => Handler();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ExplicitDelegateInvocation);
    }

    [Test]
    public async Task DirectFuncCallReportsLS0014()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                public int Run(Func<int, int> handler) => handler(7);
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ExplicitDelegateInvocation);
    }

    [Test]
    public async Task ExplicitInvokeDoesNotReport()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                public void Run(Action handler) => handler.Invoke();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task NullConditionalInvokeDoesNotReport()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                public void Run(Action? handler) => handler?.Invoke();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task RegularMethodCallDoesNotReport()
    {
        const string source =
            """
            public class Foo
            {
                private void Helper() { }
                public void Run() => Helper();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task LocalFunctionCallDoesNotReport()
    {
        const string source =
            """
            public class Foo
            {
                public void Run()
                {
                    void Local() { }
                    Local();
                }
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task QualifiedDirectDelegateCallReportsLS0014()
    {
        const string source =
            """
            using System;

            public class Bag
            {
                public Action Handler { get; init; } = () => { };
            }

            public class Foo
            {
                public void Run(Bag bag) => bag.Handler();
            }
            """;

        var diagnostics = await AnalyzerHarness.GetAnalyzerDiagnosticsAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            source);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo(DiagnosticIds.ExplicitDelegateInvocation);
    }
}
