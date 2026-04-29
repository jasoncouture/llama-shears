; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category           | Severity | Notes
--------|--------------------|----------|------------------------------------------------------------------------------------
LS0001  | LlamaShears.Style  | Error    | Primary constructors are not allowed on non-record types.
LS0002  | LlamaShears.Style  | Error    | Fields must be private (const fields exempt).
LS0003  | LlamaShears.Style  | Error    | Field names must start with an underscore (const fields exempt).
