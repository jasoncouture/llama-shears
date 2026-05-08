using LlamaShears.Core.Abstractions.Paths;

namespace LlamaShears.Core.Paths;

public sealed class FileProtectionPolicyOptions
{
    public IList<ProtectedFile> Rules { get; } = [];
}
