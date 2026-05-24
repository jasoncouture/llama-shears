using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.PromptContext;

namespace LlamaShears.UnitTests.PromptContext;

public sealed class SkillParserTests
{
    private const string CreateSkillDocument = """
---
name: create-skill
description: Teaches the agent how to write its own skills. Use when instructed to write, create, or generate a new skill.
---

To write a new skill, generate a markdown file with YAML frontmatter that defines the skill's metadata, followed by the execution instructions or code.

Provide the output using the exact structure below. Replace the bracketed placeholders with the specific logic for the requested skill.

```markdown
---
name: <insert-skill-name>
description: <insert-detailed-description-and-trigger-conditions>
---

<insert-step-by-step-instructions-or-executable-code-blocks>
```
""";

    [Test]
    public async Task ParsesCreateSkillFrontmatter()
    {
        ISkillParser parser = new SkillParser();

        var record = parser.Parse(CreateSkillDocument, "/skills/create-skill/SKILL.md");

        await Assert.That(record).IsNotNull();
        await Assert.That(record!.Name).IsEqualTo("create-skill");
        await Assert.That(record.Description)
            .IsEqualTo("Teaches the agent how to write its own skills. Use when instructed to write, create, or generate a new skill.");
        await Assert.That(record.Path).IsEqualTo("/skills/create-skill/SKILL.md");
    }

    [Test]
    public async Task BodyStripsFrontmatterAndPreservesFencedBlock()
    {
        ISkillParser parser = new SkillParser();

        var record = parser.Parse(CreateSkillDocument, "/skills/create-skill/SKILL.md");

        await Assert.That(record).IsNotNull();
        await Assert.That(record!.Body).StartsWith("To write a new skill,");
        await Assert.That(record.Body).Contains("```markdown");
        await Assert.That(record.Body).Contains("<insert-skill-name>");
        await Assert.That(record.Body).DoesNotContain("name: create-skill");
    }

    [Test]
    public async Task MetadataAndExtraPropertiesEmptyWhenAbsent()
    {
        ISkillParser parser = new SkillParser();

        var record = parser.Parse(CreateSkillDocument, "/skills/create-skill/SKILL.md");

        await Assert.That(record).IsNotNull();
        await Assert.That(record!.Metadata.Count).IsEqualTo(0);
        await Assert.That(record.ExtraProperties.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ReturnsNullWhenNoFrontmatter()
    {
        ISkillParser parser = new SkillParser();

        var record = parser.Parse("Just body text with no frontmatter at all.", "/skills/nope/SKILL.md");

        await Assert.That(record).IsNull();
    }

    [Test]
    public async Task ThrowsWhenNameMissing()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
description: missing name
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/x/SKILL.md")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenDescriptionMissing()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: only-name
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/x/SKILL.md")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CapturesMetadataAndExtraProperties()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: rich
description: rich frontmatter
version: 2
metadata:
  author: jc
  tags:
    - alpha
    - beta
---

body
""";

        var record = parser.Parse(doc, "/skills/rich/SKILL.md");

        await Assert.That(record).IsNotNull();
        await Assert.That(record!.ExtraProperties.ContainsKey("version")).IsTrue();
        await Assert.That(record.Metadata.ContainsKey("author")).IsTrue();
        await Assert.That(record.Metadata["author"]).IsEqualTo("jc");
    }

    [Test]
    public async Task ThrowsWhenNameMismatchesParentDirectory()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: create-skill
description: doc body
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/some-other-dir/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenNameHasUppercase()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: BadName
description: doc body
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/BadName/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenNameStartsWithHyphen()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: -leading
description: doc body
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/-leading/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenNameEndsWithHyphen()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: trailing-
description: doc body
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/trailing-/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenNameHasConsecutiveHyphens()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: double--hyphen
description: doc body
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/double--hyphen/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenNameExceedsMaxLength()
    {
        ISkillParser parser = new SkillParser();
        var name = new string('a', 65);
        var doc = $"""
---
name: {name}
description: doc body
---

body
""";

        await Assert.That(() => parser.Parse(doc, $"/skills/{name}/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AcceptsNameAtMaxLength()
    {
        ISkillParser parser = new SkillParser();
        var name = new string('a', 64);
        var doc = $"""
---
name: {name}
description: doc body
---

body
""";

        var record = parser.Parse(doc, $"/skills/{name}/SKILL.md");

        await Assert.That(record).IsNotNull();
        await Assert.That(record!.Name).IsEqualTo(name);
    }

    [Test]
    public async Task ThrowsWhenDescriptionEmpty()
    {
        ISkillParser parser = new SkillParser();
        const string doc = """
---
name: emptydesc
description: ""
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/emptydesc/SKILL.md"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenDescriptionExceedsMaxLength()
    {
        ISkillParser parser = new SkillParser();
        var description = new string('x', 1025);
        var doc = $"""
---
name: longdesc
description: {description}
---

body
""";

        await Assert.That(() => parser.Parse(doc, "/skills/longdesc/SKILL.md"))
            .Throws<InvalidOperationException>();
    }
}
