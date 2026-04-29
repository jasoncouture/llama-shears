using System;
using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

/// <summary>
/// Reports a hard, non-configurable error for any field whose name
/// does not start with <c>_</c>. <c>const</c> fields are exempt
/// because they are compile-time constants, not state, and
/// conventionally use PascalCase.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FieldMustStartWithUnderscoreAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.FieldMustStartWithUnderscore];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var field = (FieldDeclarationSyntax)context.Node;
        if (field.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            return;
        }

        foreach (var variable in field.Declaration.Variables)
        {
            var name = variable.Identifier.ValueText;
            if (!name.StartsWith("_", StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.FieldMustStartWithUnderscore,
                    variable.Identifier.GetLocation(),
                    name));
            }
        }
    }
}
