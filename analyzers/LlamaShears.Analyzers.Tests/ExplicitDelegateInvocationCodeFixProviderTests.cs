using LlamaShears.Analyzers.CodeFixes;

namespace LlamaShears.Analyzers.Tests;

public sealed class ExplicitDelegateInvocationCodeFixProviderTests
{
    [Test]
    public async Task RewritesNonNullableActionFieldCallToInvoke()
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

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            new ExplicitDelegateInvocationCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("_handler.Invoke()");
        await Assert.That(fixed_).DoesNotContain("_handler?.Invoke");
    }

    [Test]
    public async Task RewritesNullableActionParameterCallToConditionalInvoke()
    {
        const string source =
            """
            #nullable enable
            using System;

            public class Foo
            {
                public void Run(Action? handler) => handler();
            }
            """;

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            new ExplicitDelegateInvocationCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("handler?.Invoke()");
    }

    [Test]
    public async Task PreservesArgumentsOnRewrite()
    {
        const string source =
            """
            using System;

            public class Foo
            {
                public int Run(Func<int, int, int> handler) => handler(2, 3);
            }
            """;

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            new ExplicitDelegateInvocationCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("handler.Invoke(2, 3)");
    }

    [Test]
    public async Task RewritesQualifiedDelegatePropertyCall()
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

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new ExplicitDelegateInvocationAnalyzer(),
            new ExplicitDelegateInvocationCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("bag.Handler.Invoke()");
    }
}
