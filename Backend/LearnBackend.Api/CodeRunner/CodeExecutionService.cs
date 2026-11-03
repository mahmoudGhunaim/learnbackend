using System.Text.Json;
using LearnBackend.Api.Content;
using Microsoft.CodeAnalysis;

namespace LearnBackend.Api.CodeRunner;

/// <summary>
/// Orchestrates one run: compile → safety check → execute in the sandbox → parse results.
/// Controllers/endpoints only talk to this class; they don't know Roslyn or processes exist.
/// </summary>
public sealed class CodeExecutionService(CodeCompiler compiler, SandboxProcessRunner sandbox, ICompilerErrorCatalog errorCatalog)
{
    private const string Marker = "@@LEARN@@";

    /// <summary>Compiles the learner's code with the hidden tests and runs them.</summary>
    public async Task<SubmissionResult> RunTestsAsync(string code, string testsCode, CancellationToken ct)
    {
        var compiled = compiler.Compile(code, testsCode);
        var diagnostics = MapDiagnostics(compiled);

        if (!compiled.Success)
        {
            var outcome = compiled.Violations.Count > 0 ? RunOutcome.Blocked : RunOutcome.CompileError;
            return new SubmissionResult(outcome, diagnostics, [], 0, 0, 0, null);
        }

        // Each test may print up to 4000 chars, so test runs get a larger output budget.
        var run = await sandbox.RunAsync(compiled.Assembly!, ct, maxOutputChars: 400_000);
        var (planned, results) = ParseHarnessOutput(run.Stdout);

        var tests = planned.Select(p =>
        {
            if (results.TryGetValue(p.Name, out var r))
                return new TestCaseResult(p.Name, p.Ar, p.En, ParseStatus(r.Status), r.Message, r.Expected, r.Actual,
                    r.ExceptionType, r.Line, string.IsNullOrEmpty(r.Output) ? null : r.Output, r.Ms);
            // No result line: the process was killed or crashed before reaching this test.
            var status = run.TimedOut ? TestStatus.Timeout : TestStatus.NotRun;
            return new TestCaseResult(p.Name, p.Ar, p.En, status, null, null, null, null, null, null, 0);
        }).ToList();

        var passed = tests.Count(t => t.Status == TestStatus.Passed);
        var finalOutcome = run switch
        {
            { TimedOut: true } => RunOutcome.Timeout,
            { MemoryExceeded: true } => RunOutcome.MemoryLimit,
            _ when planned.Count == 0 || results.Count < planned.Count => RunOutcome.Crash,
            _ when passed == tests.Count => RunOutcome.Passed,
            _ => RunOutcome.Failed,
        };

        return new SubmissionResult(finalOutcome, diagnostics, tests, passed, tests.Count, run.ElapsedMs,
            finalOutcome is RunOutcome.Crash or RunOutcome.MemoryLimit ? DescribeCrash(run) : null);
    }

    /// <summary>Compiles and runs a normal program (playground). Returns what it printed.</summary>
    public async Task<ProgramRunResult> RunProgramAsync(string code, CancellationToken ct)
    {
        var compiled = compiler.Compile(code, testsCode: null);
        var diagnostics = MapDiagnostics(compiled);
        if (!compiled.Success)
        {
            var outcome = compiled.Violations.Count > 0 ? RunOutcome.Blocked : RunOutcome.CompileError;
            return new ProgramRunResult(outcome, diagnostics, "", null, false, 0);
        }

        var run = await sandbox.RunAsync(compiled.Assembly!, ct);
        var outcome2 = run switch
        {
            { TimedOut: true } => RunOutcome.Timeout,
            { MemoryExceeded: true } => RunOutcome.MemoryLimit,
            { ExitCode: not 0 } => RunOutcome.Crash,
            _ => RunOutcome.Completed,
        };
        var error = outcome2 == RunOutcome.Completed ? null : (run.Stderr.Length > 0 ? CleanStackTrace(run.Stderr) : DescribeCrash(run));
        return new ProgramRunResult(outcome2, diagnostics, run.Stdout, error, run.OutputTruncated, run.ElapsedMs);
    }

    private List<CodeDiagnostic> MapDiagnostics(CompilationOutput compiled)
    {
        var list = new List<CodeDiagnostic>();

        foreach (var d in compiled.UserDiagnostics)
        {
            var span = d.Location.GetLineSpan();
            var info = errorCatalog.Find(d.Id);
            list.Add(new CodeDiagnostic(d.Id, Severity(d), DiagnosticSource.User,
                span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1, span.EndLinePosition.Character + 1,
                d.GetMessage(System.Globalization.CultureInfo.InvariantCulture), info?.Ar, info?.TipAr, info?.TipEn));
        }

        // Errors inside the hidden tests almost always mean "your public API doesn't match what
        // the exercise asked for" (wrong class/method name or signature). Line numbers are hidden.
        foreach (var d in compiled.TestDiagnostics.DistinctBy(d => d.GetMessage()))
        {
            var info = errorCatalog.Find(d.Id);
            list.Add(new CodeDiagnostic(d.Id, "error", DiagnosticSource.Tests, 0, 0, 0, 0,
                d.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
                "الاختبارات المخفية لم تستطع استخدام الكود الخاص بك. تأكد أن أسماء الأصناف والدوال وأنواعها تطابق ما طلبه التمرين بالضبط. " + (info?.Ar ?? ""),
                info?.TipAr, "The hidden tests could not use your code: check that class/method names and signatures match the exercise exactly."));
        }

        foreach (var v in compiled.Violations)
            list.Add(new CodeDiagnostic("SANDBOX", "error", DiagnosticSource.Safety, v.Line, v.Column, v.Line, v.Column + 1,
                $"'{v.Api}': {v.ReasonEn}", v.ReasonAr, null, null));

        return list;
    }

    private static string Severity(Diagnostic d) => d.Severity == DiagnosticSeverity.Error ? "error" : "warning";

    private static TestStatus ParseStatus(string status) => status switch
    {
        "passed" => TestStatus.Passed,
        "failed" => TestStatus.Failed,
        "timeout" => TestStatus.Timeout,
        _ => TestStatus.Error,
    };

    private static string DescribeCrash(SandboxRunOutput run)
    {
        if (run.MemoryExceeded) return "Memory limit exceeded (OutOfMemory). Look for collections that grow forever.";
        if (run.Stderr.Contains("Stack overflow", StringComparison.OrdinalIgnoreCase))
            return "Stack overflow: a method calls itself forever (infinite recursion). A property that returns itself (e.g. `public int X => X;`) does this too.";
        if (run.OutputTruncated) return "Too much output: the program printed more than the allowed limit.";
        return string.IsNullOrWhiteSpace(run.Stderr) ? $"The process exited unexpectedly (exit code {run.ExitCode})." : CleanStackTrace(run.Stderr);
    }

    /// <summary>Keeps only the stack frames from the learner's file, which are the interesting ones.</summary>
    private static string CleanStackTrace(string stderr)
    {
        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var kept = lines.Where((l, i) => i == 0 || l.Contains(CodeCompiler.UserFileName, StringComparison.Ordinal))
            .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"in .*Solution\.cs:line (\d+)", "in Solution.cs:line $1"));
        return string.Join('\n', kept);
    }

    private sealed record PlannedTest(string Name, string Ar, string En);
    private sealed record HarnessResult(string Name, string Status, string? Message, string? Expected, string? Actual,
        string? ExceptionType, int? Line, string? Output, long Ms);

    private static (List<PlannedTest> Planned, Dictionary<string, HarnessResult> Results) ParseHarnessOutput(string stdout)
    {
        var planned = new List<PlannedTest>();
        var results = new Dictionary<string, HarnessResult>();
        foreach (var raw in stdout.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (!line.StartsWith(Marker, StringComparison.Ordinal)) continue;
            using var doc = JsonDocument.Parse(line[Marker.Length..]);
            var kind = doc.RootElement.GetProperty("kind").GetString();
            if (kind == "plan")
                planned = doc.RootElement.GetProperty("tests").Deserialize<List<PlannedTest>>(ContentStore.JsonOptions) ?? [];
            else if (kind == "result" && doc.RootElement.Deserialize<HarnessResult>(ContentStore.JsonOptions) is { } r)
                results[r.Name] = r;
        }
        return (planned, results);
    }
}
