using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace LlamaShears.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExplicitDelegateInvocationCodeFixProvider))]
[Shared]
public sealed class ExplicitDelegateInvocationCodeFixProvider : CodeFixProvider
{
    private const string Title = "Invoke delegate via .Invoke";

    public override ImmutableArray<string> FixableDiagnosticIds
        => [DiagnosticIds.ExplicitDelegateInvocation];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken => RewriteAsync(context.Document, invocation, cancellationToken),
                equivalenceKey: nameof(ExplicitDelegateInvocationCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> RewriteAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        var receiver = invocation.Expression;
        var receiverWithoutTrailing = receiver.WithoutTrailingTrivia();
        var argumentList = invocation.ArgumentList;

        var nullable = IsNullableReceiver(receiver, semanticModel, cancellationToken);

        ExpressionSyntax rewritten = nullable
            ? SyntaxFactory.ConditionalAccessExpression(
                receiverWithoutTrailing,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName("Invoke")),
                    argumentList))
            : SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiverWithoutTrailing,
                    SyntaxFactory.IdentifierName("Invoke")),
                argumentList);

        rewritten = rewritten
            .WithLeadingTrivia(invocation.GetLeadingTrivia())
            .WithTrailingTrivia(invocation.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root!.ReplaceNode(invocation, rewritten);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsNullableReceiver(
        ExpressionSyntax receiver,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var typeInfo = semanticModel.GetTypeInfo(receiver, cancellationToken);
        if (typeInfo.Nullability.Annotation == NullableAnnotation.Annotated)
        {
            return true;
        }
        return typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull;
    }
}
