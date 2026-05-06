using System;
using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoCancellationTokenAbbreviationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.NoCancellationTokenAbbreviation];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLocal, SyntaxKind.LocalDeclarationStatement);
        context.RegisterSyntaxNodeAction(AnalyzeForEach, SyntaxKind.ForEachStatement);
        context.RegisterSyntaxNodeAction(AnalyzeCatchDeclaration, SyntaxKind.CatchDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeSingleVariableDesignation, SyntaxKind.SingleVariableDesignation);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;
        ReportIfBanned(context, parameter.Identifier);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var field = (FieldDeclarationSyntax)context.Node;
        foreach (var variable in field.Declaration.Variables)
        {
            ReportIfBanned(context, variable.Identifier);
        }
    }

    private static void AnalyzeLocal(SyntaxNodeAnalysisContext context)
    {
        var local = (LocalDeclarationStatementSyntax)context.Node;
        foreach (var variable in local.Declaration.Variables)
        {
            ReportIfBanned(context, variable.Identifier);
        }
    }

    private static void AnalyzeForEach(SyntaxNodeAnalysisContext context)
    {
        var forEach = (ForEachStatementSyntax)context.Node;
        ReportIfBanned(context, forEach.Identifier);
    }

    private static void AnalyzeCatchDeclaration(SyntaxNodeAnalysisContext context)
    {
        var catchDecl = (CatchDeclarationSyntax)context.Node;
        if (!catchDecl.Identifier.IsKind(SyntaxKind.None))
        {
            ReportIfBanned(context, catchDecl.Identifier);
        }
    }

    private static void AnalyzeSingleVariableDesignation(SyntaxNodeAnalysisContext context)
    {
        var designation = (SingleVariableDesignationSyntax)context.Node;
        ReportIfBanned(context, designation.Identifier);
    }

    private static void ReportIfBanned(SyntaxNodeAnalysisContext context, SyntaxToken identifier)
    {
        var name = identifier.ValueText;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        if (!IsBannedAbbreviation(name))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.NoCancellationTokenAbbreviation,
            identifier.GetLocation(),
            name));
    }

    private static bool IsBannedAbbreviation(string name)
    {
        var trimmed = name.TrimStart('_');
        return string.Equals(trimmed, "ct", StringComparison.OrdinalIgnoreCase);
    }
}
