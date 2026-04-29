namespace LlamaShears.Hosting.Abstractions;

/// <summary>
/// Well-known on-disk locations for LlamaShears user state. Per
/// ADR-0011, everything lives under the user-profile dotfile root on
/// every platform — no per-OS branch.
/// </summary>
public static class LlamaShearsPaths
{
    /// <summary>
    /// Root directory for all LlamaShears user state on the local
    /// machine. Resolves to <c>~/.llama-shears</c> on Linux/macOS and
    /// <c>%USERPROFILE%\.llama-shears</c> on Windows.
    /// </summary>
    public static string DataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".llama-shears");

    /// <summary>
    /// Path to the host's primary JSON configuration file,
    /// <c>&lt;DataRoot&gt;/config.json</c>. The host treats this as
    /// optional and reloads it on change.
    /// </summary>
    public static string ConfigFile => Path.Combine(DataRoot, "config.json");
}
