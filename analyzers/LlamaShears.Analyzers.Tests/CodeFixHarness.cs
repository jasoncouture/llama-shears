using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace LlamaShears.Analyzers.Tests;

/// <summary>
/// Minimal harness for applying a <see cref="CodeFixProvider"/> to a
/// short C# source string. Pairs with <see cref="AnalyzerHarness"/>:
/// the fix is invoked against the first diagnostic produced by the
/// supplied analyzer and the resulting document text is returned for
/// comparison.
/// </summary>
internal static class CodeFixHarness
{
    private static readonly MetadataReference[] DefaultReferences = BuildDefaultReferences();

    public static async Task<string> ApplyFixAsync(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider fixProvider,
        string source,
        CancellationToken cancellationToken = default)
    {
        var workspace = new AdhocWorkspace(MefHostServices.DefaultHost);
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(projectId, DefaultReferences)
            .AddDocument(documentId, "Test.cs", source);

        var document = solution.GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var withAnalyzers = compilation!.WithAnalyzers([analyzer]);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

        var fixable = diagnostics.FirstOrDefault(d => fixProvider.FixableDiagnosticIds.Contains(d.Id))
            ?? throw new InvalidOperationException("No fixable diagnostic produced by analyzer.");

        CodeAction? registered = null;
        var fixContext = new CodeFixContext(
            document,
            fixable,
            (action, _) => registered ??= action,
            cancellationToken);
        await fixProvider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        if (registered is null)
        {
            throw new InvalidOperationException("Fix provider did not register a code action.");
        }

        var operations = await registered.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var changedSolution = applyChanges.ChangedSolution;
        var changedDocument = changedSolution.GetDocument(documentId)!;
        var formatted = await Formatter.FormatAsync(changedDocument, Formatter.Annotation, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var text = await formatted.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return text.ToString();
    }

    public static async Task<(string OriginalText, string AddedFileName, string AddedText)> ApplyFixAndCollectAddedDocumentAsync(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider fixProvider,
        string source,
        CancellationToken cancellationToken = default)
    {
        var workspace = new AdhocWorkspace(MefHostServices.DefaultHost);
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(projectId, DefaultReferences)
            .AddDocument(documentId, "Test.cs", source);

        var document = solution.GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var withAnalyzers = compilation!.WithAnalyzers([analyzer]);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

        var fixable = diagnostics.FirstOrDefault(d => fixProvider.FixableDiagnosticIds.Contains(d.Id))
            ?? throw new InvalidOperationException("No fixable diagnostic produced by analyzer.");

        CodeAction? registered = null;
        var fixContext = new CodeFixContext(
            document,
            fixable,
            (action, _) => registered ??= action,
            cancellationToken);
        await fixProvider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        if (registered is null)
        {
            throw new InvalidOperationException("Fix provider did not register a code action.");
        }

        var operations = await registered.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var changedSolution = applyChanges.ChangedSolution;

        var addedDocumentId = changedSolution.GetProject(projectId)!.DocumentIds
            .Except([documentId])
            .Single();

        var originalDocument = changedSolution.GetDocument(documentId)!;
        var originalFormatted = await Formatter.FormatAsync(originalDocument, Formatter.Annotation, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var originalText = (await originalFormatted.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();

        var addedDocument = changedSolution.GetDocument(addedDocumentId)!;
        var addedFormatted = await Formatter.FormatAsync(addedDocument, Formatter.Annotation, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var addedText = (await addedFormatted.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();

        return (originalText, addedDocument.Name, addedText);
    }

    private static MetadataReference[] BuildDefaultReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return
        [
            .. trustedAssemblies
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)),
        ];
    }
}
