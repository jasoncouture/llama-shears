using LlamaShears.Agent.Core.SystemPrompt;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class SystemPromptDataProviderTests
{
    [Test]
    public async Task BuildReturnsModelWithRequiredSectionsPopulated()
    {
        var provider = new SystemPromptDataProvider(new FakeTimeProvider(DateTimeOffset.UnixEpoch));

        var model = provider.Build(new FakeAgent());

        await Assert.That(model.Workspace).IsNotNull();
        await Assert.That(model.Tools).IsNotNull();
        await Assert.That(model.Runtime).IsNotNull();
    }

    [Test]
    public async Task BuildLeavesSectionAndSubagentNull()
    {
        var provider = new SystemPromptDataProvider(new FakeTimeProvider(DateTimeOffset.UnixEpoch));

        var model = provider.Build(new FakeAgent());

        await Assert.That(model.Section).IsNull();
        await Assert.That(model.Subagent).IsNull();
    }

    [Test]
    public async Task BuildSetsTimezoneFromTimeProvider()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("UTC"));
        var provider = new SystemPromptDataProvider(time);

        var model = provider.Build(new FakeAgent());

        await Assert.That(model.Runtime.Timezone).IsEqualTo(TimeZoneInfo.FindSystemTimeZoneById("UTC"));
    }

    [Test]
    public async Task BuildThrowsForNullAgent()
    {
        var provider = new SystemPromptDataProvider(new FakeTimeProvider(DateTimeOffset.UnixEpoch));

        await Assert.That(() => provider.Build(null!))
            .Throws<ArgumentNullException>();
    }
}
