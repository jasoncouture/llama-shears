using System.Collections.Generic;

namespace LlamaShears.DocsBuild;

internal sealed class TypeRef
{
    public string FullName { get; set; } = "";

    public List<TypeRef> GenericArgs { get; } = [];

    public int ArrayRank { get; set; }

    public bool IsByRef { get; set; }

    public bool IsPointer { get; set; }
}
