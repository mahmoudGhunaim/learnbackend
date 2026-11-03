using LearnBackend.Tests.Infrastructure;

namespace LearnBackend.Tests;

public class SandboxSmokeTests
{
    [Fact]
    public async Task Runs_a_simple_program_and_captures_output()
    {
        var compiled = Runner.Compiler.Compile("Console.WriteLine($\"Hello {1 + 2}\");", testsCode: null);
        Assert.True(compiled.Success, string.Join("\n", compiled.UserDiagnostics));

        var run = await Runner.Sandbox.RunAsync(compiled.Assembly!, CancellationToken.None);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("Hello 3", run.Stdout.Trim());
    }
}
