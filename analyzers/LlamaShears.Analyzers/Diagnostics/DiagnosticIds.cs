namespace LlamaShears.Analyzers.Diagnostics;

/// <summary>
/// Stable identifiers for every diagnostic and suppression produced by
/// the LlamaShears analyzers. Numeric ranges:
/// <list type="bullet">
///   <item><c>LS0001</c>–<c>LS0999</c>: hard-error analyzer rules.</item>
///   <item><c>LSSPR0001</c>–<c>LSSPR0999</c>: suppressions of
///   third-party diagnostics.</item>
/// </list>
/// </summary>
internal static class DiagnosticIds
{
    public const string PrimaryConstructorOnNonRecord = "LS0001";

    public const string NoPublicOrInternalFields = "LS0002";

    public const string SuppressIde0290 = "LSSPR0001";
}
