using System.Runtime.InteropServices;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.PromptContext;

internal sealed class HostDataProvider : IDataContextItemProvider
{
    private readonly KeyValuePair<string, object?>[] _items;

    public HostDataProvider()
    {
        var data = new HostData(
            Hostname: Environment.MachineName,
            Username: Environment.UserName,
            OperatingSystem: RuntimeInformation.OSDescription,
            RuntimeIdentifier: RuntimeInformation.RuntimeIdentifier,
            ProcessorArchitecture: RuntimeInformation.OSArchitecture.ToString());
        _items = [new KeyValuePair<string, object?>(HostData.DataKey, data)];
    }

    public Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<KeyValuePair<string, object?>>>(_items);
}
