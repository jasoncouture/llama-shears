using System.Collections.Immutable;
using System.Threading;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

/// <summary>
/// Reports a configurable warning whenever an extension method is
/// invoked through <c>this.</c> from inside the receiver's own type.
/// The <c>this.</c> qualifier is required by the language here (so
/// <see cref="NoThisQualifierAnalyzer"/> permits it), but the pattern
/// is a smell: the type is reaching out to an external static for
/// behavior that operates on the instance. Suppress where the
/// external extension is intentional.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExtensionMethodOnThisAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Descriptors.ExtensionMethodOnThis];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || memberAccess.Expression is not ThisExpressionSyntax)
        {
            return;
        }

        if (!IsExtensionMethodInvocation(context.SemanticModel, invocation, context.CancellationToken, out var methodName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ExtensionMethodOnThis,
            memberAccess.Name.GetLocation(),
            methodName));
    }

    private static bool IsExtensionMethodInvocation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken,
        out string methodName)
    {
        methodName = string.Empty;
        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
        {
            return false;
        }

        if (!symbol.IsExtensionMethod && symbol.ReducedFrom is null)
        {
            return false;
        }

        methodName = symbol.Name;
        return true;
    }
}
