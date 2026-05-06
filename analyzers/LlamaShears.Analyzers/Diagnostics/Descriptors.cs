using Microsoft.CodeAnalysis;

namespace LlamaShears.Analyzers.Diagnostics;

internal static class Descriptors
{
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

    // This one only exists because claude, even on opus 4.7, cannot for whatever reason, fatom this instruction.
    // So now it's a build error, to get it to knock it the fuck off.
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

    public static readonly DiagnosticDescriptor XmlDocOnConcreteType = new(
        id: DiagnosticIds.XmlDocOnConcreteType,
        title: "XML doc comment on concrete type or member",
        messageFormat: "'{0}' has an XML doc comment; XML doc comments on concrete types and their members are the exception, not the rule",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "ADR-0012: XML doc comments on concrete types and their members are the exception, " +
            "not the rule. If the target's name or signature is not self-describing, the preferred " +
            "fix is to refactor it to be self-describing — not to add a doc comment. " +
            "<inheritdoc/> on interface implementations is exempt.");

    public static readonly DiagnosticDescriptor PublicInterfaceMissingXmlDoc = new(
        id: DiagnosticIds.PublicInterfaceMissingXmlDoc,
        title: "Public interface is missing an XML doc comment",
        messageFormat: "Public interface '{0}' is missing an XML doc comment; public interface contracts must be documented",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "ADR-0012: XML doc comments are required on public interfaces. " +
            "The interface is the contract; document it for callers and implementers.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    public static readonly DiagnosticDescriptor NonPublicInterfaceMissingXmlDoc = new(
        id: DiagnosticIds.NonPublicInterfaceMissingXmlDoc,
        title: "Non-public interface is missing an XML doc comment",
        messageFormat: "Interface '{0}' is missing an XML doc comment; interface contracts should be documented",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "ADR-0012: XML doc comments are expected on interfaces because the interface is the " +
            "contract. Severity is reduced from error to warning for non-public interfaces because " +
            "their audience is internal.");

    public static readonly DiagnosticDescriptor PublicInterfaceMemberMissingXmlDoc = new(
        id: DiagnosticIds.PublicInterfaceMemberMissingXmlDoc,
        title: "Public interface member is missing an XML doc comment",
        messageFormat: "Public interface member '{0}' is missing an XML doc comment; public interface contracts must be documented",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "ADR-0012: XML doc comments are required on members of public interfaces. " +
            "The interface is the contract; document each member for callers and implementers.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    public static readonly DiagnosticDescriptor NonPublicInterfaceMemberMissingXmlDoc = new(
        id: DiagnosticIds.NonPublicInterfaceMemberMissingXmlDoc,
        title: "Non-public interface member is missing an XML doc comment",
        messageFormat: "Interface member '{0}' is missing an XML doc comment; interface contracts should be documented",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "ADR-0012: XML doc comments are expected on members of interfaces because the " +
            "interface is the contract. Severity is reduced from error to warning for members of " +
            "non-public interfaces because their audience is internal.");

    public static readonly DiagnosticDescriptor DocumentationModeNotDiagnose = new(
        id: DiagnosticIds.DocumentationModeNotDiagnose,
        title: "DocumentationMode must be Diagnose for the XML doc analyzer to work",
        messageFormat:
            "C# DocumentationMode is not 'Diagnose'. Without it, Roslyn parses '///' as ordinary " +
            "comments and the XML doc analyzer (LS0008/0009/0010/0011/0012) cannot detect doc " +
            "comments. Add <GenerateDocumentationFile>true</GenerateDocumentationFile> to the " +
            "project's .csproj.",
        category: DiagnosticCategories.Style,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Roslyn parses '///' as either DocumentationCommentTrivia or ordinary comment trivia " +
            "depending on CSharpParseOptions.DocumentationMode. The XML doc analyzer relies on " +
            "DocumentationMode = Diagnose, which the .NET SDK enables when " +
            "<GenerateDocumentationFile> is true. Without it, the analyzer cannot reliably detect " +
            "doc comments and would report false 'missing doc' errors on every documented type.",
        customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    public static readonly SuppressionDescriptor SuppressIde0290 = new(
        id: DiagnosticIds.SuppressIde0290,
        suppressedDiagnosticId: "IDE0290",
        justification:
            "LlamaShears policy: primary constructors are forbidden on non-record types. " +
            "The suggestion is replaced by the LS0001 hard-error analyzer, which enforces the inverse rule.");

    public static readonly SuppressionDescriptor SuppressCs1591 = new(
        id: DiagnosticIds.SuppressCs1591,
        suppressedDiagnosticId: "CS1591",
        justification:
            "ADR-0012: XML doc comments default to absent; concrete public types and members " +
            "should be self-describing rather than carry redundant doc comments. CS1591 (missing " +
            "XML comment for publicly visible type or member) contradicts that policy. The " +
            "missing-doc requirement on public interfaces is enforced by LS0009/LS0011 instead.");
}
