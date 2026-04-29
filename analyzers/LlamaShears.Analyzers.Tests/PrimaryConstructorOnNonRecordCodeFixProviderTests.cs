using LlamaShears.Analyzers.CodeFixes;

namespace LlamaShears.Analyzers.Tests;

public sealed class PrimaryConstructorOnNonRecordCodeFixProviderTests
{
    [Test]
    public async Task Generates_readonly_field_and_constructor_for_single_parameter_class()
    {
        const string source =
            """
            public class Foo(int x)
            {
                public int X => x;
            }
            """;

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            new PrimaryConstructorOnNonRecordCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("private readonly int _x;");
        await Assert.That(fixed_).Contains("public Foo(int x)");
        await Assert.That(fixed_).Contains("_x = x;");
        await Assert.That(fixed_).Contains("public int X => _x;");
        await Assert.That(fixed_).DoesNotContain("class Foo(int x)");
    }

    [Test]
    public async Task Generates_one_field_per_parameter_for_multi_parameter_class()
    {
        const string source =
            """
            public class Foo(int x, string y)
            {
                public int X => x;
                public string Y => y;
            }
            """;

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            new PrimaryConstructorOnNonRecordCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("private readonly int _x;");
        await Assert.That(fixed_).Contains("private readonly string _y;");
        await Assert.That(fixed_).Contains("_x = x;");
        await Assert.That(fixed_).Contains("_y = y;");
        await Assert.That(fixed_).Contains("public int X => _x;");
        await Assert.That(fixed_).Contains("public string Y => _y;");
    }

    [Test]
    public async Task Forwards_primary_base_call_to_explicit_base_initializer()
    {
        const string source =
            """
            public class Base
            {
                public Base(int v) { }
            }

            public class Derived(int x) : Base(x)
            {
            }
            """;

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            new PrimaryConstructorOnNonRecordCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("public Derived(int x) : base(x)");
        await Assert.That(fixed_).DoesNotContain("Base(x)\n{");
    }

    [Test]
    public async Task Generates_field_and_constructor_for_struct()
    {
        const string source =
            """
            public struct Bar(int x)
            {
                public int X => x;
            }
            """;

        var fixed_ = await CodeFixHarness.ApplyFixAsync(
            new PrimaryConstructorOnNonRecordAnalyzer(),
            new PrimaryConstructorOnNonRecordCodeFixProvider(),
            source);

        await Assert.That(fixed_).Contains("private readonly int _x;");
        await Assert.That(fixed_).Contains("public Bar(int x)");
        await Assert.That(fixed_).Contains("public int X => _x;");
    }
}
