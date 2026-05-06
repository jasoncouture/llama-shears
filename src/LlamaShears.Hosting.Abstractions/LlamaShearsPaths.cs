namespace LlamaShears.Hosting.Abstractions;

public static class LlamaShearsPaths
{
    public const string EnvironmentVariablePrefix = "LLAMA_SHEARS";
    public const string DataRootEnvironmentVariableName = $"{EnvironmentVariablePrefix}_DATA_ROOT";
    public const string WorkspaceRootEnvironmentVariableName = $"{EnvironmentVariablePrefix}_WORKSPACE_ROOT";

    private static string? _dataRoot;
    private static string? _configPath;
    private static string? _workspaceRoot;

    /// <summary>
    /// Root for persistent host state. Subsystems compose their own
    /// paths under this.
    /// </summary>
    public static string DataRoot => _dataRoot ??= CreateDataRootPath();

    public static string ConfigFile => _configPath ??= Path.Combine(DataRoot, "config.json");

    public static string WorkspaceRoot => _workspaceRoot ??= CreateWorkspaceRootPath();

    private static string GetPathFromEnvironmentOrDefault(string environmentVariable, Func<string> defaultPathFactory)
    {
        var path = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            path = defaultPathFactory();
        }
        return Directory.CreateDirectory(path).FullName;
    }

    private static string CreateDataRootPath() =>
        GetPathFromEnvironmentOrDefault(
            DataRootEnvironmentVariableName,
            () => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".llama-shears"));

    private static string CreateWorkspaceRootPath() =>
        GetPathFromEnvironmentOrDefault(
            WorkspaceRootEnvironmentVariableName,
            () => Path.Combine(DataRoot, "workspace"));
}
