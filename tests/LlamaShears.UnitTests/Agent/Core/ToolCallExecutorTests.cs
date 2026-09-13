using System.Collections.Immutable;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class ToolCallExecutorTests
{
    [Test]
    public async Task DispatchesIndependentToolCallsConcurrently()
    {
        // Regression: ToolCallExecutor awaited each DispatchAsync in a
        // for-loop, so two independent file_reads on one turn ran serially
        // even though the harness advertises concurrent dispatch.
        var dispatcher = new BarrierDispatcher();
        var executor = CreateExecutor(dispatcher);
        var session = new SessionId("alice", SessionId.DefaultSessionName);

        var turns = await executor.ExecuteAsync(
            [new ToolCall("llamashears", "file_read", "{}", "1"), new ToolCall("llamashears", "file_read", "{}", "2")],
            [new ToolGroup("llamashears", [])],
            session,
            Guid.CreateVersion7(),
            channelId: "test",
            turnSessionId: null,
            CancellationToken.None,
            CancellationToken.None);

        await Assert.That(turns.Length).IsEqualTo(2);
        await Assert.That(dispatcher.Started).IsEqualTo(2);
    }

    [Test]
    public async Task PersistsResultTurnsInOriginalCallOrder()
    {
        var dispatcher = new SecondFinishesFirstDispatcher();
        var executor = CreateExecutor(dispatcher);
        var session = new SessionId("alice", SessionId.DefaultSessionName);

        var turns = await executor.ExecuteAsync(
            [new ToolCall("llamashears", "file_read", "{}", "1"), new ToolCall("llamashears", "file_read", "{}", "2")],
            [new ToolGroup("llamashears", [])],
            session,
            Guid.CreateVersion7(),
            channelId: "test",
            turnSessionId: null,
            CancellationToken.None,
            CancellationToken.None);

        await Assert.That(turns.Select(turn => turn.Content).ToArray()).IsEquivalentTo(["first", "second"]);
        await Assert.That(turns[0].ToolCall?.CallId).IsEqualTo("1");
        await Assert.That(turns[1].ToolCall?.CallId).IsEqualTo("2");
    }

    private static ToolCallExecutor CreateExecutor(IToolCallDispatcher dispatcher) =>
        new(
            Substitute.For<IEventBus>(),
            dispatcher,
            TimeProvider.System,
            NullLogger<ToolCallExecutor>.Instance);

    private sealed class BarrierDispatcher : IToolCallDispatcher
    {
        private int _started;
        private readonly TaskCompletionSource _bothStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Started => Volatile.Read(ref _started);

        public async ValueTask<ToolCallResult> DispatchAsync(
            ToolCall call,
            ImmutableArray<ToolGroup> tools,
            string eventId,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _started) == 2)
            {
                _bothStarted.TrySetResult();
            }

            await _bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            return new ToolCallResult("ok", IsError: false);
        }
    }

    private sealed class SecondFinishesFirstDispatcher : IToolCallDispatcher
    {
        private readonly TaskCompletionSource _secondFinished =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<ToolCallResult> DispatchAsync(
            ToolCall call,
            ImmutableArray<ToolGroup> tools,
            string eventId,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            if (call.CallId == "2")
            {
                _secondFinished.TrySetResult();
                return new ToolCallResult("second", IsError: false);
            }

            await _secondFinished.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            return new ToolCallResult("first", IsError: false);
        }
    }
}
