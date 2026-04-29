namespace LlamaShears.Analyzers.Diagnostics;

internal static class DiagnosticIds
{
    public const string PrimaryConstructorOnNonRecord = "LS0001";

    public const string NoPublicOrInternalFields = "LS0002";

    public const string FieldMustStartWithUnderscore = "LS0003";

    public const string NoThisQualifier = "LS0004";

    public const string OneTypePerFile = "LS0005";

    public const string ExtensionMethodOnThis = "LS0006";

    public const string NoCancellationTokenAbbreviation = "LS0007";

    public const string SuppressIde0290 = "LSSPR0001";
}
