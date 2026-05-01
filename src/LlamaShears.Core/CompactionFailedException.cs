namespace LlamaShears.Core;

public sealed class CompactionFailedException : Exception
{
    public CompactionFailedException(string message) : base(message)
    {
    }

    public CompactionFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
