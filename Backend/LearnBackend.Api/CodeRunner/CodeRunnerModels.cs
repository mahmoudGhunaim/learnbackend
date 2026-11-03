namespace LearnBackend.Api.CodeRunner;

/// <summary>
/// Settings for the code sandbox, bound from the "Sandbox" section of appsettings.json.
/// This is the Options pattern: a plain class + builder.Services.Configure&lt;SandboxOptions&gt;(...).
/// </summary>
public sealed class SandboxOptions
{
    public const string SectionName = "Sandbox";

    public int TimeoutSeconds { get; set; } = 5;
    public int MemoryLimitMb { get; set; } = 256;
    public int MaxOutputChars { get; set; } = 20_000;
    public int MaxConcurrentRuns { get; set; } = 4;
    public int MaxCodeLength { get; set; } = 50_000;

    /// <summary>Optional override; by default the worker DLL next to the API's own DLL is used.</summary>
    public string? WorkerPath { get; set; }
}

/// <summary>Where a diagnostic came from: your code, the hidden tests, or the safety analyzer.</summary>
public enum DiagnosticSource { User, Tests, Safety }

public sealed record CodeDiagnostic(
    string Id,
    string Severity,          // "error" | "warning"
    DiagnosticSource Source,
    int Line,
    int Column,
    int EndLine,
    int EndColumn,
    string Message,           // original English compiler message
    string? MessageAr,        // Arabic explanation (from content/compiler-errors.json)
    string? TipAr,
    string? TipEn);

public enum TestStatus { Passed, Failed, Error, Timeout, NotRun }

public sealed record TestCaseResult(
    string Name,
    string Ar,
    string En,
    TestStatus Status,
    string? Message,
    string? Expected,
    string? Actual,
    string? ExceptionType,
    int? Line,
    string? Output,
    long Ms);

public enum RunOutcome { Passed, Failed, CompileError, Blocked, Timeout, MemoryLimit, Crash, Completed }

public sealed record SubmissionResult(
    RunOutcome Outcome,
    IReadOnlyList<CodeDiagnostic> Diagnostics,
    IReadOnlyList<TestCaseResult> Tests,
    int PassedCount,
    int TotalCount,
    long ElapsedMs,
    string? Error);

public sealed record ProgramRunResult(
    RunOutcome Outcome,
    IReadOnlyList<CodeDiagnostic> Diagnostics,
    string Output,
    string? Error,
    bool Truncated,
    long ElapsedMs);
