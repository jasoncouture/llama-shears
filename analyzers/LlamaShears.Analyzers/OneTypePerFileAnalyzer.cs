using System.Collections.Immutable;
using System.Linq;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

/// <summary>
/// Reports a hard, non-configurable error when a single C# file
/// declares more than one top-level type. Top-level types are
/// declarations directly under <see cref="CompilationUnitSyntax"/> or
/// <see cref="BaseNamespaceDeclarationSyntax"/> — classes, structs,
/// interfaces, enums, records, record structs, and delegates. Nested
/// types are exempt because they are part of the outer type's
/// declaration. The first top-level type in the file is treated as
/// the "primary" type and is left alone; every additional top-level
/// type is reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneTypePerFileAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.OneTypePerFile];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var topLevelTypes = root
            .DescendantNodes(descendIntoChildren: static n =>
                n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .Where(static n => n is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .Cast<MemberDeclarationSyntax>()
            .ToList();

        if (topLevelTypes.Count <= 1)
        {
            return;
        }

        foreach (var typeDeclaration in topLevelTypes.Skip(1))
        {
            var (location, name) = GetIdentifier(typeDeclaration);
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.OneTypePerFile,
                location,
                name));
        }
    }

    private static (Location Location, string Name) GetIdentifier(MemberDeclarationSyntax declaration)
        => declaration switch
        {
            BaseTypeDeclarationSyntax t => (t.Identifier.GetLocation(), t.Identifier.ValueText),
            DelegateDeclarationSyntax d => (d.Identifier.GetLocation(), d.Identifier.ValueText),
            _ => (declaration.GetLocation(), "<unknown>"),
        };
}
