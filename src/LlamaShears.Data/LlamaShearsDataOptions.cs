namespace LlamaShears.Data;

/// <summary>
/// Configuration bound from the data section of host configuration.
/// </summary>
public sealed class LlamaShearsDataOptions
{
    /// <summary>
    /// SQLite connection string used by <see cref="LlamaShearsDbContext"/>.
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=llamashears.db";
}
