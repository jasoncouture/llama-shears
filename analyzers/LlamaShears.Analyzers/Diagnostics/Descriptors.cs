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
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    /// <summary>
    /// LS0002 — fields must be private. Public, internal, protected,
    /// protected internal, and private protected fields are all
    /// rejected; expose state through properties or methods.
    /// <c>const</c> fields are exempt — they are compile-time
    /// constants, not state.
    /// </summary>
    public static readonly DiagnosticDescriptor NoPublicOrInternalFields = new(
        id: DiagnosticIds.NoPublicOrInternalFields,
        title: "Fields must be private",
        messageFormat: "Field '{0}' is {1}; only private fields are allowed",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy: only private instance and static fields are permitted. " +
            "Expose state via properties or methods. Const fields are exempt because they are compile-time " +
            "constants, not state.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    /// <summary>
    /// LS0003 — every field name must start with an underscore.
    /// <c>const</c> fields are exempt because they are compile-time
    /// constants, not state, and conventionally use PascalCase.
    /// </summary>
    public static readonly DiagnosticDescriptor FieldMustStartWithUnderscore = new(
        id: DiagnosticIds.FieldMustStartWithUnderscore,
        title: "Field names must start with an underscore",
        messageFormat: "Field '{0}' must start with an underscore",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy: every field name must start with '_'. " +
            "Const fields are exempt because they are compile-time constants, not state, " +
            "and conventionally use PascalCase.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    /// <summary>
    /// LS0004 — <c>this.</c> qualifier is forbidden, except where it
    /// is required to call an extension method on the current
    /// instance (the only context where the qualifier is not
    /// syntactic noise).
    /// </summary>
    public static readonly DiagnosticDescriptor NoThisQualifier = new(
        id: DiagnosticIds.NoThisQualifier,
        title: "Forbidden 'this.' qualifier",
        messageFormat: "'this.' is not permitted; remove the qualifier",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy: 'this.' is forbidden except where it is required to invoke an " +
            "extension method on the current instance. Field names start with '_' (LS0003), so " +
            "there is never a field-vs-parameter shadow that 'this.' would be needed to disambiguate.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

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
