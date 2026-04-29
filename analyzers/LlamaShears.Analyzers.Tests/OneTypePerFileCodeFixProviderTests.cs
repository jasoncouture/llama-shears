using LlamaShears.Analyzers.CodeFixes;

namespace LlamaShears.Analyzers.Tests;

public sealed class OneTypePerFileCodeFixProviderTests
{
    [Test]
    public async Task ExtractsSecondClassIntoASiblingDocument()
    {
        const string source =
            """
            namespace N;

            public class Foo { }
            public class Bar { }
            """;

        var (originalText, extractedFileName, extractedText) = await CodeFixHarness.ApplyFixAndCollectAddedDocumentAsync(
            new OneTypePerFileAnalyzer(),
            new OneTypePerFileCodeFixProvider(),
            source);

        await Assert.That(extractedFileName).IsEqualTo("Bar.cs");
        await Assert.That(extractedText).Contains("namespace N");
        await Assert.That(extractedText).Contains("public class Bar");
        await Assert.That(extractedText).DoesNotContain("public class Foo");

        await Assert.That(originalText).Contains("public class Foo");
        await Assert.That(originalText).DoesNotContain("public class Bar");
    }

    [Test]
    public async Task ExtractedFileCarriesUsingDirectivesFromOriginal()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;

            namespace N;

            public class Foo { }
            public class Bar
            {
                public List<string> Items { get; } = new();
            }
            """;

        var (_, extractedFileName, extractedText) = await CodeFixHarness.ApplyFixAndCollectAddedDocumentAsync(
            new OneTypePerFileAnalyzer(),
            new OneTypePerFileCodeFixProvider(),
            source);

        await Assert.That(extractedFileName).IsEqualTo("Bar.cs");
        await Assert.That(extractedText).Contains("using System;");
        await Assert.That(extractedText).Contains("using System.Collections.Generic;");
        await Assert.That(extractedText).Contains("public class Bar");
    }

    [Test]
    public async Task ExtractsTopLevelDelegateIntoItsOwnFile()
    {
        const string source =
            """
            namespace N;

            public class Foo { }
            public delegate void Handler(int x);
            """;

        var (originalText, extractedFileName, extractedText) = await CodeFixHarness.ApplyFixAndCollectAddedDocumentAsync(
            new OneTypePerFileAnalyzer(),
            new OneTypePerFileCodeFixProvider(),
            source);

        await Assert.That(extractedFileName).IsEqualTo("Handler.cs");
        await Assert.That(extractedText).Contains("delegate void Handler(int x)");
        await Assert.That(originalText).DoesNotContain("delegate void Handler");
    }
}
