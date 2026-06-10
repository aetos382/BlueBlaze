namespace BlueBlaze.LexiconGenerator.Core;

public enum DiagnosticSeverity
{
    Error,
    Warning
}

public sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? FilePath,
    string? Nsid,
    string? DefKey);
