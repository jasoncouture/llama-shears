using System.Collections.Immutable;
using LlamaShears.Api.Web.Services.SlashCommands;
using LlamaShears.Core.Abstractions.Commands;

namespace LlamaShears.UnitTests.Services.SlashCommands;

public sealed class SlashCommandRegistryTests
{
    [Test]
    public async Task FindReturnsRegisteredCommand()
    {
        var foo = new StubCommand("/foo");
        var bar = new StubCommand("/bar");
        var registry = new SlashCommandRegistry([foo, bar]);

        await Assert.That(registry.Find("/foo")).IsSameReferenceAs(foo);
        await Assert.That(registry.Find("/bar")).IsSameReferenceAs(bar);
    }

    [Test]
    public async Task FindIsCaseInsensitive()
    {
        var foo = new StubCommand("/Foo");
        var registry = new SlashCommandRegistry([foo]);

        await Assert.That(registry.Find("/foo")).IsSameReferenceAs(foo);
        await Assert.That(registry.Find("/FOO")).IsSameReferenceAs(foo);
        await Assert.That(registry.Find("/FoO")).IsSameReferenceAs(foo);
    }

    [Test]
    public async Task FindReturnsNullWhenAbsent()
    {
        var registry = new SlashCommandRegistry([new StubCommand("/foo")]);

        await Assert.That(registry.Find("/missing")).IsNull();
    }

    [Test]
    public async Task FindReturnsNullForNullOrEmpty()
    {
        var registry = new SlashCommandRegistry([new StubCommand("/foo")]);

        await Assert.That(registry.Find(string.Empty)).IsNull();
    }

    [Test]
    public async Task CommandsPreservesRegistrationOrder()
    {
        var a = new StubCommand("/a");
        var b = new StubCommand("/b");
        var c = new StubCommand("/c");

        var registry = new SlashCommandRegistry([a, b, c]);

        await Assert.That(registry.Commands.Length).IsEqualTo(3);
        await Assert.That(registry.Commands[0]).IsSameReferenceAs(a);
        await Assert.That(registry.Commands[1]).IsSameReferenceAs(b);
        await Assert.That(registry.Commands[2]).IsSameReferenceAs(c);
    }

    private sealed class StubCommand : ISlashCommand
    {
        public StubCommand(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description => string.Empty;
        public ImmutableArray<SlashCommandParameter> Parameters => [];
        public Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken) =>
            Task.FromResult(SlashCommandResult.Default);
    }
}
