using System.Diagnostics;
using LearnBackend.Tests.Infrastructure;

namespace LearnBackend.Tests;

/// <summary>
/// Proves the sandbox actually stops dangerous or runaway code.
/// Layer 1 (compile time): SafetyAnalyzer rejects dangerous APIs.
/// Layer 2 (run time): the worker process is killed on timeout / memory / output limits.
/// </summary>
public class SandboxSecurityTests
{
    [Theory]
    [InlineData("System.IO.File.ReadAllText(\"C:/Windows/win.ini\");", "System.IO.File")]
    [InlineData("using F = System.IO.File; F.Delete(\"x\");", "System.IO.File")]
    [InlineData("var d = new System.IO.DirectoryInfo(\".\");", "System.IO.DirectoryInfo")]
    [InlineData("Environment.Exit(0);", "System.Environment.Exit")]
    [InlineData("var v = Environment.GetEnvironmentVariable(\"PATH\");", "System.Environment.GetEnvironmentVariable")]
    [InlineData("var m = typeof(string).GetMethod(\"Trim\", Type.EmptyTypes);", "System.Type.GetMethod")]
    [InlineData("var t = Type.GetType(\"System.IO.File\");", "System.Type.GetType")]
    [InlineData("var a = AppDomain.CurrentDomain;", "System.AppDomain")]
    [InlineData("var o = Activator.CreateInstance(typeof(object));", "System.Activator")]
    [InlineData("var s = Console.OpenStandardOutput();", "System.Console.OpenStandardOutput")]
    [InlineData("Func<int> f = () => 1; var mi = f.Method;", "System.Delegate.Method")]
    [InlineData("var asm = typeof(string).Assembly;", "System.Type.Assembly")]
    public void Dangerous_apis_are_blocked_at_compile_time(string code, string expectedApi)
    {
        var compiled = Runner.Compiler.Compile(code, testsCode: null);

        Assert.False(compiled.Success);
        Assert.Contains(compiled.Violations, v => v.Api.StartsWith(expectedApi, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("var c = new System.Net.Http.HttpClient();")]
    [InlineData("System.Diagnostics.Process.Start(\"cmd.exe\");")]
    [InlineData("unsafe { int x = 1; int* p = &x; }")]
    public void Apis_outside_the_reference_allow_list_do_not_compile(string code)
    {
        var compiled = Runner.Compiler.Compile(code, testsCode: null);

        Assert.False(compiled.Success);
        Assert.NotEmpty(compiled.UserDiagnostics);
    }

    [Fact]
    public void Safe_code_with_lookalike_names_is_allowed()
    {
        const string code = """
            using System.Diagnostics;
            using System.Text;
            var sw = Stopwatch.StartNew();
            var sb = new StringBuilder();
            var writer = new System.IO.StringWriter();
            writer.Write("ok");
            Console.WriteLine(Environment.NewLine + typeof(int).Name + 42.GetType().Name + sw.ElapsedMilliseconds.ToString().Length);
            record File(string Path);
            """;
        var compiled = Runner.Compiler.Compile(code, testsCode: null);

        Assert.True(compiled.Success, string.Join("\n", compiled.UserDiagnostics.Select(d => d.ToString()).Concat(compiled.Violations.Select(v => v.Api))));
    }

    [Fact]
    public async Task Infinite_loop_is_killed_by_the_timeout()
    {
        var compiled = Runner.Compiler.Compile("while (true) { }", testsCode: null);
        var sw = Stopwatch.StartNew();

        var run = await Runner.Sandbox.RunAsync(compiled.Assembly!, CancellationToken.None);

        Assert.True(run.TimedOut);
        Assert.InRange(sw.Elapsed.TotalSeconds, 4.5, 9);
    }

    [Fact]
    public async Task Memory_bomb_is_stopped()
    {
        const string code = """
            var hog = new List<byte[]>();
            while (true) hog.Add(new byte[10_000_000]);
            """;
        var compiled = Runner.Compiler.Compile(code, testsCode: null);

        var run = await Runner.Sandbox.RunAsync(compiled.Assembly!, CancellationToken.None);

        Assert.True(run.MemoryExceeded, $"exit={run.ExitCode} stderr={run.Stderr}");
        Assert.False(run.TimedOut);
    }

    [Fact]
    public async Task Output_flood_is_truncated()
    {
        var compiled = Runner.Compiler.Compile("while (true) Console.WriteLine(\"spam spam spam spam\");", testsCode: null);

        var run = await Runner.Sandbox.RunAsync(compiled.Assembly!, CancellationToken.None);

        Assert.True(run.OutputTruncated);
        Assert.True(run.Stdout.Length <= 20_000);
    }

    [Fact]
    public async Task Stack_overflow_crashes_only_the_worker()
    {
        const string code = """
            Console.WriteLine(Recurse(1));
            static int Recurse(int n) => Recurse(n + 1) + 1;
            """;
        var compiled = Runner.Compiler.Compile(code, testsCode: null);

        var run = await Runner.Sandbox.RunAsync(compiled.Assembly!, CancellationToken.None);

        Assert.NotEqual(0, run.ExitCode);
        Assert.Contains("Stack overflow", run.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_errors_keep_the_learners_line_numbers()
    {
        const string code = "int a = 1;\nint b = 2;\nint c = \"three\";\n";
        var compiled = Runner.Compiler.Compile(code, testsCode: null);

        var error = Assert.Single(compiled.UserDiagnostics, d => d.Id == "CS0029");
        Assert.Equal(2, error.Location.GetLineSpan().StartLinePosition.Line); // 0-based → line 3
    }
}
