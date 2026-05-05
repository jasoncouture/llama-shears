using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Context;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed record ModelContextProtocolToolSet(
    string ServerName,
    Uri ServerUri,
    ImmutableArray<ToolDescriptor> Tools);
