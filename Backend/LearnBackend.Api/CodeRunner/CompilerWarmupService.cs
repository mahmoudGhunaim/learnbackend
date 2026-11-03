namespace LearnBackend.Api.CodeRunner;

/// <summary>
/// A hosted (background) service: ASP.NET Core starts it together with the web server.
/// Roslyn is slow the very first time it compiles (its own code is being JIT-compiled),
/// so we compile a tiny program in the background at startup instead of making
/// your first click on "Run" wait for it.
/// </summary>
public sealed class CompilerWarmupService(CodeCompiler compiler, ILogger<CompilerWarmupService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() =>
    {
        var started = TimeProvider.System.GetTimestamp();
        compiler.Compile("Console.WriteLine(\"warm-up\");", testsCode: null);
        logger.LogInformation("Roslyn warm-up finished in {Elapsed}", TimeProvider.System.GetElapsedTime(started));
    }, stoppingToken);
}
