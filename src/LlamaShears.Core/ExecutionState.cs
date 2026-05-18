namespace LlamaShears.Core;

public static class ExecutionState
{
    private static ExecutionContext? _cleanSlateContext;
    public static async ValueTask<ExecutionContext> CreateBlankContextAsync()
    {
        if (_cleanSlateContext is not null) return _cleanSlateContext.CreateCopy();
        Task<ExecutionContext> task;
        AsyncFlowControl flowControl = default;
        try
        {
            // Step 1: If flow control is not suppressed, we need to suppress it.
            if (!ExecutionContext.IsFlowSuppressed())
            {
                flowControl = ExecutionContext.SuppressFlow();
            }
            // Then we start a task, that we will use to "steal" a blank execution context.
            task = Task.Factory.StartNew(CaptureAndReturnContext);
        }
        finally
        {
            if (flowControl != default)
            {
                flowControl.Undo();
            }
        }
        return _cleanSlateContext = (_cleanSlateContext = await task).CreateCopy();
    }

    private static ExecutionContext CaptureAndReturnContext()
    {
        // Step 1: Create an async local
        var local = new AsyncLocal<object?>();
        // Step 2: and populate it. This ensures that the execution context initializes.
        local.Value = new object();
        // Step 3: Capture a copy of our execution context
        var result = ExecutionContext.Capture()?.CreateCopy() ??
               throw new InvalidOperationException("Unable to capture execution context!");
        GC.KeepAlive(local); // Prevent the JIT from deciding our side-effecting code doesn't matter.
        // And throw our new execution context back as a return value.
        return result;
    }
}
