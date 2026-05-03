using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

public sealed record ModelTokenInformationContextEntry(int TokenCount) : IContextEntry
{

}
