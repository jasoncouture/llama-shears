using LlamaShears.Agent.Abstractions.Events;
using LlamaShears.Agent.Core.Channels;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class UiInputChannelTests
{
    [Test]
    public async Task ReadAsyncYieldsNothingWhenNoMessagesHaveArrived()
    {
        await using var provider = BuildServices();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);

        var turns = await ReadAllAsync(channel);

        await Assert.That(turns).IsEmpty();
    }

    [Test]
    public async Task ReadAsyncWithASingleMessageYieldsItVerbatim()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        var at = DateTimeOffset.UtcNow;
        await publisher.PublishAsync(new UserMessageSubmitted("alice", "hello", at), CancellationToken.None);

        var turns = await ReadAllAsync(channel);

        await Assert.That(turns).Count().IsEqualTo(1);
        await Assert.That(turns[0].Role).IsEqualTo(ModelRole.User);
        await Assert.That(turns[0].Content).IsEqualTo("hello");
        await Assert.That(turns[0].Timestamp).IsEqualTo(at);
    }

    [Test]
    public async Task ReadAsyncWithMultipleMessagesYieldsASingleCoalescedTurnInOrder()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        var t0 = DateTimeOffset.UtcNow;
        await publisher.PublishAsync(new UserMessageSubmitted("alice", "first", t0), CancellationToken.None);
        await publisher.PublishAsync(new UserMessageSubmitted("alice", "second", t0.AddSeconds(1)), CancellationToken.None);
        await publisher.PublishAsync(new UserMessageSubmitted("alice", "third", t0.AddSeconds(2)), CancellationToken.None);

        var turns = await ReadAllAsync(channel);

        await Assert.That(turns).Count().IsEqualTo(1);
        await Assert.That(turns[0].Role).IsEqualTo(ModelRole.User);
        var content = turns[0].Content;
        await Assert.That(content).Contains("first");
        await Assert.That(content).Contains("second");
        await Assert.That(content).Contains("third");
        await Assert.That(content.IndexOf("first", StringComparison.Ordinal))
            .IsLessThan(content.IndexOf("second", StringComparison.Ordinal));
        await Assert.That(content.IndexOf("second", StringComparison.Ordinal))
            .IsLessThan(content.IndexOf("third", StringComparison.Ordinal));
    }

    [Test]
    public async Task ReadAsyncAfterDrainYieldsNothingUntilANewMessageArrives()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        await publisher.PublishAsync(new UserMessageSubmitted("alice", "hello", DateTimeOffset.UtcNow), CancellationToken.None);
        _ = await ReadAllAsync(channel);

        var second = await ReadAllAsync(channel);

        await Assert.That(second).IsEmpty();
    }

    [Test]
    public async Task MessagesForOtherAgentsAreIgnored()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        await publisher.PublishAsync(new UserMessageSubmitted("bob", "not for alice", DateTimeOffset.UtcNow), CancellationToken.None);

        var turns = await ReadAllAsync(channel);

        await Assert.That(turns).IsEmpty();
    }

    [Test]
    public async Task WaitForInputAsyncCompletesWhenAMatchingMessageArrives()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        var wait = channel.WaitForInputAsync(CancellationToken.None);

        await Assert.That(wait.IsCompleted).IsFalse();

        await publisher.PublishAsync(new UserMessageSubmitted("alice", "hello", DateTimeOffset.UtcNow), CancellationToken.None);

        var completed = await Task.WhenAny(wait, Task.Delay(2000));
        await Assert.That(completed).IsSameReferenceAs(wait);
    }

    [Test]
    public async Task WaitForInputAsyncDoesNotCompleteForOtherAgentsMessages()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        var wait = channel.WaitForInputAsync(CancellationToken.None);
        await publisher.PublishAsync(new UserMessageSubmitted("bob", "noise", DateTimeOffset.UtcNow), CancellationToken.None);

        var completed = await Task.WhenAny(wait, Task.Delay(150));
        await Assert.That(completed).IsNotSameReferenceAs(wait);
    }

    [Test]
    public async Task WaitForInputAsyncCompletesImmediatelyIfMessagesAreAlreadyPending()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<UserMessageSubmitted>>();

        using var channel = new UiInputChannel("alice", subscriber);
        await publisher.PublishAsync(new UserMessageSubmitted("alice", "hello", DateTimeOffset.UtcNow), CancellationToken.None);

        var wait = channel.WaitForInputAsync(CancellationToken.None);

        await Assert.That(wait.IsCompleted).IsTrue();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        return services.BuildServiceProvider();
    }

    private static async Task<IReadOnlyList<ModelTurn>> ReadAllAsync(UiInputChannel channel)
    {
        var collected = new List<ModelTurn>();
        await foreach (var turn in channel.ReadAsync(CancellationToken.None))
        {
            collected.Add(turn);
        }
        return collected;
    }
}
