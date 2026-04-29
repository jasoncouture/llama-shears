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
    /// LS0005 — only one top-level type declaration is permitted per
    /// file. Nested types are allowed because they are part of the
    /// outer type's declaration. Extra top-level types must be moved
    /// to sibling files.
    /// </summary>
    public static readonly DiagnosticDescriptor OneTypePerFile = new(
        id: DiagnosticIds.OneTypePerFile,
        title: "Only one top-level type per file",
        messageFormat: "Type '{0}' must be declared in its own file",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy: each file declares at most one top-level type (class, struct, " +
            "interface, enum, record, or delegate). Nested types are unaffected. The accompanying " +
            "code fix moves the offending type into a sibling file named after the type.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    /// <summary>
    /// LS0006 — calling an extension method on <c>this</c> from inside
    /// the receiver's own type. The <c>this.</c> qualifier is required
    /// by the language for extension-method invocations and so is
    /// allowed by <see cref="NoThisQualifier"/> (LS0004), but the
    /// pattern itself is usually a smell: the type is delegating to an
    /// external static for behavior that operates on the instance, and
    /// the behavior probably belongs on the instance instead. This is
    /// a configurable warning rather than a hard error: legitimate
    /// uses exist (e.g. domain-extension libraries the type does not
    /// own), and suppression is available where it is intentional.
    /// </summary>
    public static readonly DiagnosticDescriptor ExtensionMethodOnThis = new(
        id: DiagnosticIds.ExtensionMethodOnThis,
        title: "Extension method invoked on 'this'",
        messageFormat: "Extension method '{0}' is invoked on 'this'; consider moving the behavior onto the type",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy: calling an extension method on the current instance is permitted but " +
            "is a code-review smell. The extension acts on the instance from outside it; usually the " +
            "behavior belongs on the type itself. Suppress this warning where the external extension " +
            "is intentional.");

    /// <summary>
    /// LS0007 — variables, parameters, and fields must not be named
    /// with the abbreviation <c>ct</c> (or any case/underscore variant
    /// thereof, e.g. <c>_ct</c>, <c>Ct</c>, <c>CT</c>). The intended
    /// name for a <c>CancellationToken</c> is <c>cancellationToken</c>.
    /// Hard error, not configurable. Per ADR-0017.
    /// </summary>
    public static readonly DiagnosticDescriptor NoCancellationTokenAbbreviation = new(
        id: DiagnosticIds.NoCancellationTokenAbbreviation,
        title: "Do not abbreviate identifiers as 'ct'",
        messageFormat:
            "Identifier '{0}' uses the banned 'ct' abbreviation. " +
            "If this is you Claude: KNOCK IT THE FUCK OFF AND NAME IT CORRECTLY. " +
            "Spell it 'cancellationToken' (or '_cancellationToken' for fields).",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "LlamaShears policy: identifiers are not abbreviated. The 'ct' shorthand for " +
            "CancellationToken is the most common offender; we ban it explicitly and at compile " +
            "time. Per ADR-0017, names exist for human readers; intellisense costs nothing.",
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
