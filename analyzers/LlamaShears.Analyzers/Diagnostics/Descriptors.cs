using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Diagnostics;

/// <summary>
/// Central registry for every <see cref="DiagnosticDescriptor"/> and
/// <see cref="SuppressionDescriptor"/> shipped by the LlamaShears
/// analyzer assembly. Analyzers and suppressors must consume their
/// descriptors from here rather than redefining them locally; this
/// keeps ids, titles, severities, and justifications in one
/// authoritative place.
/// </summary>
internal static class Descriptors
{
    /// <summary>
    /// LS0001 — primary constructors are not allowed on non-record
    /// classes or structs. Hard error, not configurable.
    /// </summary>
    public static readonly DiagnosticDescriptor PrimaryConstructorOnNonRecord = new(
        id: DiagnosticIds.PrimaryConstructorOnNonRecord,
        title: "Primary constructors are not allowed on non-record types",
        messageFormat: "Type '{0}' uses a primary constructor; primary constructors are only allowed on records",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy forbids primary constructors on classes and structs. " +
            "Convert the type to a record or move the parameters into an explicit constructor.",
        customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });

    /// <summary>
    /// LSSPR0001 — unconditionally suppresses IDE0290 ("Use primary
    /// constructor"). The project enforces the inverse rule via
    /// <see cref="PrimaryConstructorOnNonRecord"/>.
    /// </summary>
    public static readonly SuppressionDescriptor SuppressIde0290 = new(
        id: DiagnosticIds.SuppressIde0290,
        suppressedDiagnosticId: "IDE0290",
        justification:
            "LlamaShears policy: primary constructors are forbidden on non-record types. " +
            "The suggestion is replaced by the LS0001 hard-error analyzer, which enforces the inverse rule.");
}
