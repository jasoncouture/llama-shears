using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

/// <summary>
/// Reports a hard, non-configurable error for any non-private field
/// declaration. Public, internal, protected, and protected-internal
/// fields are all rejected; only <c>private</c> and
/// <c>private protected</c> fields are allowed. <c>const</c> fields
/// are exempt — they are compile-time constants rather than state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoPublicOrInternalFieldsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.NoPublicOrInternalFields];

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
            var symbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken)
                as IFieldSymbol;
            if (symbol is null)
            {
                continue;
            }

            if (IsForbidden(symbol.DeclaredAccessibility))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.NoPublicOrInternalFields,
                    variable.Identifier.GetLocation(),
                    variable.Identifier.ValueText,
                    Describe(symbol.DeclaredAccessibility)));
            }
        }
    }

    private static bool IsForbidden(Accessibility accessibility)
        => accessibility is not (Accessibility.Private or Accessibility.NotApplicable);

    private static string Describe(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        _ => accessibility.ToString().ToLowerInvariant(),
    };
}
