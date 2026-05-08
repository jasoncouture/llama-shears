namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Expands a possibly-shorthand path to an absolute path.
/// </summary>
public interface IPathExpander
{
    /// <summary>
    /// Expands <paramref name="path"/> to an absolute path:
    /// <list type="bullet">
    /// <item><description>If <paramref name="path"/> is absolute, it is returned unchanged.</description></item>
    /// <item><description>If <paramref name="path"/> is a bare <c>~</c> or begins with <c>~/</c>, the <c>~</c> is replaced with the current user's profile directory.</description></item>
    /// <item><description>Otherwise, <paramref name="path"/> is joined with <paramref name="workingDirectory"/> and resolved via <see cref="Path.GetFullPath(string)"/>.</description></item>
    /// </list>
    /// </summary>
    string ExpandPath(string path, string workingDirectory);
}
