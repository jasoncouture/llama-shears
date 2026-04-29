using System.Collections.Immutable;
using LlamaShears.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LlamaShears.Analyzers;

/// <summary>
/// Unconditionally suppresses IDE0290 ("Use primary constructor"). The
/// LlamaShears policy is the inverse: primary constructors on
/// non-record types are a hard error
/// (<see cref="PrimaryConstructorOnNonRecordAnalyzer"/>). Suppressing
/// the built-in suggestion stops the IDE from guiding callers toward a
/// pattern that the analyzer would then reject.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuppressIde0290 : DiagnosticSuppressor
{
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
        => ImmutableArray.Create(Descriptors.SuppressIde0290);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            context.ReportSuppression(
                Suppression.Create(Descriptors.SuppressIde0290, diagnostic));
        }
    }
}
