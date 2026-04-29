using System.Collections.Immutable;
using System.Threading;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoThisQualifierAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.NoThisQualifier];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeThisExpression, SyntaxKind.ThisExpression);
    }

    private static void AnalyzeThisExpression(SyntaxNodeAnalysisContext context)
    {
        var thisExpr = (ThisExpressionSyntax)context.Node;
        if (thisExpr.Parent is not MemberAccessExpressionSyntax memberAccess
            || memberAccess.Expression != thisExpr)
        {
            return;
        }

        if (memberAccess.Parent is InvocationExpressionSyntax invocation
            && IsExtensionMethodInvocation(context.SemanticModel, invocation, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.NoThisQualifier,
            thisExpr.GetLocation()));
    }

    private static bool IsExtensionMethodInvocation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
        {
            return false;
        }

        return symbol.IsExtensionMethod || symbol.ReducedFrom is not null;
    }
}
