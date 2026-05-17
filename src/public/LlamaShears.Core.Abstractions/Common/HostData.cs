namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Host-level system info exposed on the data context under the
/// <c>host</c> key. Captured once at process start; all consumers
/// (templates, services, prompts) see the same snapshot.
/// </summary>
/// <param name="Hostname">Machine name reported by the OS at process start.</param>
/// <param name="Username">User the host process is running as.</param>
/// <param name="OperatingSystem">Human-readable OS description (e.g. <c>"Linux 6.12.10-arch1-1 #1 SMP …"</c>).</param>
/// <param name="RuntimeIdentifier">.NET runtime identifier for the host (e.g. <c>"linux-x64"</c>, <c>"win-arm64"</c>).</param>
/// <param name="ProcessorArchitecture">CPU architecture reported by the OS (e.g. <c>"X64"</c>, <c>"Arm64"</c>).</param>
public sealed record HostData(
    string Hostname,
    string Username,
    string OperatingSystem,
    string RuntimeIdentifier,
    string ProcessorArchitecture)
{
    /// <summary>Data-context key under which a <see cref="HostData"/> snapshot is stored.</summary>
    public const string DataKey = "host";
}
