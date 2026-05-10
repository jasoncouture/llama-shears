using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LlamaShears.Analyzers.CodeFixes;

internal sealed class PrimaryConstructorParameterRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly ImmutableHashSet<IParameterSymbol> _targets;

    public PrimaryConstructorParameterRewriter(
        SemanticModel semanticModel,
        ImmutableArray<IParameterSymbol> targets)
    {
        this._semanticModel = semanticModel;
        this._targets = targets.ToImmutableHashSet<IParameterSymbol>(SymbolEqualityComparer.Default);
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var info = _semanticModel.GetSymbolInfo(node);
        if (info.Symbol is IParameterSymbol parameter && _targets.Contains(parameter))
        {
            return SyntaxFactory.IdentifierName("_" + parameter.Name).WithTriviaFrom(node);
        }
        return base.VisitIdentifierName(node);
    }
}
