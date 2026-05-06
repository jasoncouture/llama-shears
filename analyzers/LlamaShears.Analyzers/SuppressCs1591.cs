using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuppressCs1591 : DiagnosticSuppressor
{
    private const string OptOutKey = "llamashears_suppress_cs1591";

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
        => [Descriptors.SuppressCs1591];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (IsSuppressionDisabled(context, diagnostic))
            {
                continue;
            }
            context.ReportSuppression(
                Suppression.Create(Descriptors.SuppressCs1591, diagnostic));
        }
    }

    private static bool IsSuppressionDisabled(
        SuppressionAnalysisContext context,
        Diagnostic diagnostic)
    {
        var tree = diagnostic.Location.SourceTree;
        if (tree is null)
        {
            return false;
        }
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
        if (!options.TryGetValue(OptOutKey, out var value))
        {
            return false;
        }
        return string.Equals(value, "false", System.StringComparison.OrdinalIgnoreCase);
    }
}
