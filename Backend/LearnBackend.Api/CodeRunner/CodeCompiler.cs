using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace LearnBackend.Api.CodeRunner;

public sealed record CompilationOutput(
    bool Success,
    byte[]? Assembly,
    IReadOnlyList<Diagnostic> UserDiagnostics,
    IReadOnlyList<Diagnostic> TestDiagnostics,
    IReadOnlyList<SafetyViolation> Violations);

/// <summary>
/// Compiles C# source text into an in-memory assembly using Roslyn (Microsoft.CodeAnalysis).
///
/// Each piece of code is its own SyntaxTree with its own "file name", which is why compiler
/// errors in YOUR code keep the exact line numbers you see in the editor:
///   Solution.cs     — your code
///   GlobalUsings.cs — the same implicit usings an SDK project gets (minus IO/Http)
///   HiddenTests.cs  — the exercise's tests (never sent to the browser)
///   Harness.cs      — the mini test runner (CodeRunner/Harness/TestHarness.cs)
/// </summary>
public sealed class CodeCompiler(ReferenceCatalog references)
{
    public const string UserFileName = "Solution.cs";

    private const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private static readonly Lazy<string> HarnessSource = new(() =>
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Harness.TestHarness.cs")
                           ?? throw new InvalidOperationException("Embedded test harness not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    /// <param name="userCode">The learner's code.</param>
    /// <param name="testsCode">Hidden tests; when null the code is compiled as a normal program (playground).</param>
    public CompilationOutput Compile(string userCode, string? testsCode)
    {
        var userTree = Parse(userCode, UserFileName);
        var trees = new List<SyntaxTree> { userTree, Parse(GlobalUsings, "GlobalUsings.cs") };
        if (testsCode is not null)
        {
            trees.Add(Parse(testsCode, "HiddenTests.cs"));
            trees.Add(Parse(HarnessSource.Value, "Harness.cs"));
        }

        var options = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            mainTypeName: testsCode is null ? null : "LearnHarness.HarnessEntry",
            optimizationLevel: OptimizationLevel.Debug,   // Debug keeps line numbers precise in stack traces
            allowUnsafe: false,
            nullableContextOptions: NullableContextOptions.Enable);

        var compilation = CSharpCompilation.Create(
            "LearnerCode_" + Guid.NewGuid().ToString("N"),
            trees,
            references.References,
            options);

        var all = compilation.GetDiagnostics()
            .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToList();

        var userDiagnostics = all.Where(d => d.Location.SourceTree == userTree || d.Location == Location.None).ToList();
        var testDiagnostics = all.Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree is { } t && t != userTree).ToList();
        var hasErrors = all.Any(d => d.Severity == DiagnosticSeverity.Error);

        // Only analyse code that compiles: the semantic model is only reliable then.
        var violations = hasErrors
            ? []
            : SafetyAnalyzer.Analyze(compilation.GetSemanticModel(userTree), userTree);

        if (hasErrors || violations.Count > 0)
            return new CompilationOutput(false, null, userDiagnostics, testDiagnostics, violations);

        using var pe = new MemoryStream();
        // Embedded PDB = debug info inside the DLL, so exceptions report "Solution.cs:line 12".
        var emit = compilation.Emit(pe, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
        if (!emit.Success)
        {
            var emitErrors = emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            return new CompilationOutput(false, null, userDiagnostics.Concat(emitErrors).ToList(), testDiagnostics, []);
        }

        return new CompilationOutput(true, pe.ToArray(), userDiagnostics, testDiagnostics, []);
    }

    private static SyntaxTree Parse(string code, string path) =>
        CSharpSyntaxTree.ParseText(SourceText.From(code, Encoding.UTF8), ParseOptions, path);
}
