; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category           | Severity | Notes
--------|--------------------|----------|------------------------------------------------------------------------------------
LS0001  | LlamaShears.Style  | Error    | Primary constructors are not allowed on non-record types.
LS0002  | LlamaShears.Style  | Error    | Fields must be private (const fields exempt).
LS0003  | LlamaShears.Style  | Error    | Field names must start with an underscore (const fields exempt).
LS0004  | LlamaShears.Style  | Error    | 'this.' qualifier is forbidden except for extension method invocations.
LS0005  | LlamaShears.Style  | Error    | Only one top-level type declaration per file (nested types unaffected).
LS0006  | LlamaShears.Style  | Warning  | Extension method invoked on 'this'; the behavior likely belongs on the type itself. Configurable.
LS0007  | LlamaShears.Style  | Error    | Identifiers may not abbreviate to 'ct' (any case/underscore variant). Spell CancellationToken parameters as 'cancellationToken'.
LS0008  | LlamaShears.Style  | Warning  | XML doc comment on a concrete type or its member; XML doc on concrete is the exception, not the rule. Configurable. <inheritdoc/> exempt.
LS0009  | LlamaShears.Style  | Error    | Public interface is missing an XML doc comment.
LS0010  | LlamaShears.Style  | Warning  | Non-public interface is missing an XML doc comment. Configurable.
LS0011  | LlamaShears.Style  | Error    | Public interface member is missing an XML doc comment.
LS0012  | LlamaShears.Style  | Warning  | Non-public interface member is missing an XML doc comment. Configurable.
LS0013  | LlamaShears.Style  | Error    | C# DocumentationMode is not Diagnose; enable <GenerateDocumentationFile>true</GenerateDocumentationFile> in the project for the XML doc analyzer to detect doc comments correctly.
