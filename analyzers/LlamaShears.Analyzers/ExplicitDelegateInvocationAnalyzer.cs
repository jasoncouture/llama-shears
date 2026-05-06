using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExplicitDelegateInvocationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.ExplicitDelegateInvocation];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (IsExplicitInvokeCall(invocation.Expression))
        {
            return;
        }

        var symbol = context.SemanticModel
            .GetSymbolInfo(invocation, context.CancellationToken)
            .Symbol as IMethodSymbol;
        if (symbol is null || symbol.MethodKind != MethodKind.DelegateInvoke)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ExplicitDelegateInvocation,
            invocation.GetLocation()));
    }

    private static bool IsExplicitInvokeCall(ExpressionSyntax expression)
        => expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoke" } => true,
            MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Invoke" } => true,
            _ => false,
        };
}
