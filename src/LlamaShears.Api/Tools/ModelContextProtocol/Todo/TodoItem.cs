namespace LlamaShears.Api.Tools.ModelContextProtocol.Todo;

public sealed record TodoItem(int Index, string Text, bool Completed)
{
    private string? _toStringCache;
    public override string ToString() => _toStringCache ??= $"{Index}. [{(Completed ? 'x' : ' ')}] {Text}";
}