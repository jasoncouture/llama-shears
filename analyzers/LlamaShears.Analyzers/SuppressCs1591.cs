using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuppressCs1591 : DiagnosticSuppressor
{
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
        => [Descriptors.SuppressCs1591];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            context.ReportSuppression(
                Suppression.Create(Descriptors.SuppressCs1591, diagnostic));
        }
    }
}
