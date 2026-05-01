using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrimaryConstructorOnNonRecordCodeFixProvider))]
[Shared]
public sealed class PrimaryConstructorOnNonRecordCodeFixProvider : CodeFixProvider
{
    private const string Title =
        "Convert primary constructor to readonly fields and explicit constructor";

    public override ImmutableArray<string> FixableDiagnosticIds
        => [DiagnosticIds.PrimaryConstructorOnNonRecord];

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
        var typeDecl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDecl is null
            || typeDecl is RecordDeclarationSyntax
            || typeDecl.ParameterList is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: ct => ConvertAsync(context.Document, typeDecl, ct),
                equivalenceKey: nameof(PrimaryConstructorOnNonRecordCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ConvertAsync(
        Document document,
        TypeDeclarationSyntax typeDecl,
        CancellationToken cancellationToken)
    {
        var paramList = typeDecl.ParameterList!;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        var paramSymbols = paramList.Parameters
            .Select(p => semanticModel.GetDeclaredSymbol(p, cancellationToken))
            .OfType<IParameterSymbol>()
            .ToImmutableArray();

        if (paramSymbols.Length != paramList.Parameters.Count)
        {
            return document;
        }

        var fieldDeclarations = paramSymbols.Select(CreateFieldDeclaration).ToArray();

        var newBaseList = typeDecl.BaseList;
        ConstructorInitializerSyntax? baseInitializer = null;
        if (typeDecl.BaseList is { } baseList)
        {
            var primaryBase = baseList.Types.OfType<PrimaryConstructorBaseTypeSyntax>().FirstOrDefault();
            if (primaryBase is not null)
            {
                baseInitializer = SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    primaryBase.ArgumentList);
                var simpleBase = SyntaxFactory.SimpleBaseType(primaryBase.Type).WithTriviaFrom(primaryBase);
                newBaseList = baseList.WithTypes(baseList.Types.Replace(primaryBase, simpleBase));
            }
        }

        var ctorBody = SyntaxFactory.Block(
            paramSymbols.Select(p =>
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("_" + p.Name),
                        SyntaxFactory.IdentifierName(p.Name)))));

        var ctorDecl = SyntaxFactory.ConstructorDeclaration(typeDecl.Identifier.WithoutTrivia())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(paramList.WithoutTrivia())
            .WithBody(ctorBody);
        if (baseInitializer is not null)
        {
            ctorDecl = ctorDecl.WithInitializer(baseInitializer);
        }

        var rewriter = new PrimaryConstructorParameterRewriter(semanticModel, paramSymbols);
        var rewrittenMembers = typeDecl.Members
            .Select(m => (MemberDeclarationSyntax)rewriter.Visit(m)!)
            .ToArray();

        var newMembers = SyntaxFactory.List<MemberDeclarationSyntax>(fieldDeclarations)
            .Add(ctorDecl)
            .AddRange(rewrittenMembers);

        var newTypeDecl = typeDecl
            .WithParameterList(null)
            .WithBaseList(newBaseList)
            .WithMembers(newMembers)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root!.ReplaceNode(typeDecl, newTypeDecl);
        return document.WithSyntaxRoot(newRoot);
    }

    private static FieldDeclarationSyntax CreateFieldDeclaration(IParameterSymbol parameter)
    {
        var typeSyntax = SyntaxFactory.ParseTypeName(
            parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var variable = SyntaxFactory.VariableDeclarator("_" + parameter.Name);
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(typeSyntax).AddVariables(variable))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }
}
