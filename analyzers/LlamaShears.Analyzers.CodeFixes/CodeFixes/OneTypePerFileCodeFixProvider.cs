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

/// <summary>
/// Code fix for <see cref="DiagnosticIds.OneTypePerFile"/>: extracts
/// the offending type into a new sibling document named
/// <c>{TypeName}.cs</c>, preserving the original file's using
/// directives and namespace structure (file-scoped or block-scoped).
/// The original document keeps the primary (first) type and any other
/// types not yet extracted; running the fix repeatedly (or via
/// FixAll) splits every extra type into its own file.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OneTypePerFileCodeFixProvider))]
[Shared]
public sealed class OneTypePerFileCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [DiagnosticIds.OneTypePerFile];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var typeDecl = node.FirstAncestorOrSelf<MemberDeclarationSyntax>(static n =>
            n is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);
        if (typeDecl is null)
        {
            return;
        }

        var typeName = GetTypeName(typeDecl);
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Move '{typeName}' to its own file",
                createChangedSolution: ct => MoveTypeAsync(context.Document, typeDecl, typeName, ct),
                equivalenceKey: nameof(OneTypePerFileCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Solution> MoveTypeAsync(
        Document document,
        MemberDeclarationSyntax typeDecl,
        string typeName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return document.Project.Solution;
        }

        var newCompilationUnit = BuildExtractedCompilationUnit(compilationUnit, typeDecl)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var rootWithoutType = (CompilationUnitSyntax)compilationUnit.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia)!;
        var documentWithoutType = document.WithSyntaxRoot(rootWithoutType);

        var newDocumentId = DocumentId.CreateNewId(document.Project.Id, debugName: typeName + ".cs");
        var newDocumentName = typeName + ".cs";
        var solution = documentWithoutType.Project.Solution.AddDocument(
            newDocumentId,
            newDocumentName,
            newCompilationUnit.GetText(),
            folders: document.Folders);

        var addedDocument = solution.GetDocument(newDocumentId)!;
        var formatted = await Formatter.FormatAsync(addedDocument, Formatter.Annotation, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return formatted.Project.Solution;
    }

    private static CompilationUnitSyntax BuildExtractedCompilationUnit(
        CompilationUnitSyntax original,
        MemberDeclarationSyntax extractedType)
    {
        var containingNamespace = extractedType.Parent as BaseNamespaceDeclarationSyntax;
        if (containingNamespace is null)
        {
            return SyntaxFactory.CompilationUnit()
                .WithExterns(original.Externs)
                .WithUsings(original.Usings)
                .WithMembers([extractedType]);
        }

        BaseNamespaceDeclarationSyntax newNamespace = containingNamespace switch
        {
            FileScopedNamespaceDeclarationSyntax fileScoped =>
                fileScoped
                    .WithMembers([extractedType])
                    .WithExterns([])
                    .WithUsings([]),
            NamespaceDeclarationSyntax block =>
                block
                    .WithMembers([extractedType])
                    .WithExterns([])
                    .WithUsings([]),
            _ => containingNamespace.WithMembers([extractedType]),
        };

        return SyntaxFactory.CompilationUnit()
            .WithExterns(original.Externs)
            .WithUsings(original.Usings)
            .WithMembers([newNamespace]);
    }

    private static string GetTypeName(MemberDeclarationSyntax declaration)
        => declaration switch
        {
            BaseTypeDeclarationSyntax t => t.Identifier.ValueText,
            DelegateDeclarationSyntax d => d.Identifier.ValueText,
            _ => "Extracted",
        };
}
