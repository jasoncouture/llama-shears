using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LlamaShears.Analyzers.CodeFixes;

/// <summary>
/// Rewrites <see cref="IdentifierNameSyntax"/> references to the
/// supplied set of primary-constructor parameter symbols, replacing
/// each with the corresponding <c>_name</c> field reference. Used by
/// <see cref="PrimaryConstructorOnNonRecordCodeFixProvider"/> while
/// converting a primary constructor into an explicit constructor plus
/// readonly fields.
/// </summary>
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
