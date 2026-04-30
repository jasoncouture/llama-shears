using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers.Tests;

/// <summary>
/// Minimal in-process harness for running a
/// <see cref="DiagnosticAnalyzer"/> over a short C# source string and
/// collecting the resulting diagnostics. Built directly on Roslyn so
/// tests do not depend on the xUnit/NUnit-flavored
/// <c>Microsoft.CodeAnalysis.Testing</c> packages.
/// </summary>
internal static class AnalyzerHarness
{
    private static readonly MetadataReference[] DefaultReferences = BuildDefaultReferences();

    /// <summary>
    /// Runs <paramref name="analyzer"/> against <paramref name="source"/>
    /// and returns every diagnostic the analyzer reported, sorted by
    /// span start.
    /// </summary>
    public static Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        CancellationToken cancellationToken = default)
        => GetAnalyzerDiagnosticsAsync(
            analyzer,
            source,
            DocumentationMode.Diagnose,
            cancellationToken);

    public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        DocumentationMode documentationMode,
        CancellationToken cancellationToken = default)
    {
        var compilation = CreateCompilation(source, documentationMode);
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);
        return diagnostics
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    /// <summary>
    /// Compiles <paramref name="source"/> with <paramref name="suppressor"/>
    /// active and returns every compiler diagnostic, including
    /// suppression-info entries. Useful for verifying that a
    /// <see cref="DiagnosticSuppressor"/> actually suppresses a target
    /// diagnostic id.
    /// </summary>
    public static async Task<ImmutableArray<Diagnostic>> GetCompilationDiagnosticsWithSuppressorAsync(
        DiagnosticSuppressor suppressor,
        string source,
        CancellationToken cancellationToken = default)
    {
        var compilation = CreateCompilation(source, DocumentationMode.Diagnose);
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(suppressor));
        return await withAnalyzers.GetAllDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static CSharpCompilation CreateCompilation(string source, DocumentationMode documentationMode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest, documentationMode));
        return CSharpCompilation.Create(
            assemblyName: "LlamaShears.Analyzers.Tests.Dynamic",
            syntaxTrees: new[] { syntaxTree },
            references: DefaultReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static MetadataReference[] BuildDefaultReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return trustedAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
