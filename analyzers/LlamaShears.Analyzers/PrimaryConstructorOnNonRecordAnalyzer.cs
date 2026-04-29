using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

/// <summary>
/// Reports a hard error whenever a primary constructor is declared on
/// a non-record class or struct. The rule is intentionally
/// non-configurable: the project policy is that primary constructors
/// are reserved for records, and any other use is a build-breaking
/// mistake. The companion <see cref="SuppressIde0290"/> turns off the
/// built-in IDE suggestion that pushes in the opposite direction.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorOnNonRecordAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.PrimaryConstructorOnNonRecord);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;
        if (declaration.ParameterList is not { } parameterList)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.PrimaryConstructorOnNonRecord,
            parameterList.GetLocation(),
            declaration.Identifier.ValueText));
    }
}
