using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LlamaShears.Analyzers.CodeFixes;

internal sealed class PrimaryConstructorParameterRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel semanticModel;
    private readonly ImmutableHashSet<IParameterSymbol> targets;

    public PrimaryConstructorParameterRewriter(
        SemanticModel semanticModel,
        ImmutableArray<IParameterSymbol> targets)
    {
        this.semanticModel = semanticModel;
        this.targets = targets.ToImmutableHashSet<IParameterSymbol>(SymbolEqualityComparer.Default);
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var info = semanticModel.GetSymbolInfo(node);
        if (info.Symbol is IParameterSymbol parameter && targets.Contains(parameter))
        {
            return SyntaxFactory.IdentifierName("_" + parameter.Name).WithTriviaFrom(node);
        }
        return base.VisitIdentifierName(node);
    }
}
