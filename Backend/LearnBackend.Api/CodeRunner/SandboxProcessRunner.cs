using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace LearnBackend.Api.CodeRunner;

public sealed record SandboxRunOutput(
    int? ExitCode,
    string Stdout,
    string Stderr,
    bool TimedOut,
    bool MemoryExceeded,
    bool OutputTruncated,
    long ElapsedMs);

/// <summary>
/// Starts LearnBackend.Sandbox as a child process, sends it the compiled assembly on stdin,
/// and enforces the limits: wall-clock timeout, memory, and output size.
/// Registered as a Singleton because it owns a SemaphoreSlim that limits concurrent runs
/// for the WHOLE application (a Scoped instance would get a new semaphore per request).
/// </summary>
public sealed class SandboxProcessRunner
{
    private readonly SandboxOptions _options;
    private readonly ILogger<SandboxProcessRunner> _logger;
    private readonly SemaphoreSlim _gate;
    private readonly string _hostPath;
    private readonly string _workerPath;

    // Only these environment variables are passed to the child; secrets in the API's
    // environment (connection strings, API keys) never reach learner code.
    private static readonly string[] InheritedEnvironment =
        ["PATH", "SystemRoot", "windir", "TEMP", "TMP", "HOME", "USERPROFILE", "DOTNET_ROOT", "DOTNET_ROOT(x86)", "ProgramFiles", "LOCALAPPDATA"];

    public SandboxProcessRunner(IOptions<SandboxOptions> options, ILogger<SandboxProcessRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
        _gate = new SemaphoreSlim(_options.MaxConcurrentRuns);
        _workerPath = _options.WorkerPath ?? Path.Combine(AppContext.BaseDirectory, "LearnBackend.Sandbox.dll");
        _hostPath = FindDotnetHost();
    }

    /// <param name="maxOutputChars">Defaults to SandboxOptions.MaxOutputChars.</param>
    public async Task<SandboxRunOutput> RunAsync(byte[] assembly, CancellationToken cancellationToken, int? maxOutputChars = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await RunCoreAsync(assembly, maxOutputChars ?? _options.MaxOutputChars, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SandboxRunOutput> RunCoreAsync(byte[] assembly, int maxOutputChars, CancellationToken cancellationToken)
    {
        var memoryLimitBytes = (long)_options.MemoryLimitMb * 1024 * 1024;
        var psi = new ProcessStartInfo(_hostPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetTempPath(),
        };
        psi.ArgumentList.Add(_workerPath);

        var inherited = InheritedEnvironment
            .Select(k => (Key: k, Value: Environment.GetEnvironmentVariable(k)))
            .Where(x => x.Value is not null)
            .ToList();
        psi.Environment.Clear();
        foreach (var (key, value) in inherited) psi.Environment[key] = value;
        psi.Environment["DOTNET_GCHeapHardLimit"] = "0x" + memoryLimitBytes.ToString("X");
        psi.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        var stopwatch = Stopwatch.StartNew();
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the sandbox process.");

        var stdout = new CappedReader(process.StandardOutput, maxOutputChars);
        var stderr = new CappedReader(process.StandardError, 8_000);
        var stdoutTask = stdout.ReadAllAsync();
        var stderrTask = stderr.ReadAllAsync();

        try
        {
            await process.StandardInput.BaseStream.WriteAsync(assembly, cancellationToken);
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // The worker died before reading everything; the exit code tells the story.
        }

        var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        // Process memory = GC heap + runtime itself (~40-60 MB), so the watchdog is a bit looser
        // than the GC limit; the GC limit is what normally stops runaway allocations.
        var watchdogLimit = memoryLimitBytes + 160L * 1024 * 1024;
        bool timedOut = false, memoryExceeded = false;

        var exited = process.WaitForExitAsync(CancellationToken.None);
        while (!exited.IsCompleted)
        {
            if (stopwatch.Elapsed > timeout) { timedOut = true; Kill(process); break; }
            if (cancellationToken.IsCancellationRequested) { Kill(process); break; }
            if (stdout.Exceeded) { Kill(process); break; }
            try
            {
                process.Refresh();
                if (process.PrivateMemorySize64 > watchdogLimit) { memoryExceeded = true; Kill(process); break; }
            }
            catch (InvalidOperationException) { /* exited between checks */ }

            await Task.WhenAny(exited, Task.Delay(40, CancellationToken.None));
        }

        await process.WaitForExitAsync(CancellationToken.None);
        stopwatch.Stop();
        var output = await stdoutTask;
        var error = await stderrTask;

        if (!memoryExceeded && error.Contains("OutOfMemory", StringComparison.Ordinal)) memoryExceeded = true;

        _logger.LogInformation("Sandbox run finished in {Elapsed} ms (exit {ExitCode}, timeout {TimedOut}, memory {Memory})",
            stopwatch.ElapsedMilliseconds, process.ExitCode, timedOut, memoryExceeded);

        return new SandboxRunOutput(
            timedOut ? null : process.ExitCode, output, error, timedOut, memoryExceeded, stdout.Exceeded,
            stopwatch.ElapsedMilliseconds);
    }

    private static void Kill(Process process)
    {
        try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
    }

    /// <summary>The dotnet host that runs the API also runs the sandbox worker.</summary>
    private static string FindDotnetHost()
    {
        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        if (Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } hostPath && File.Exists(hostPath))
            return hostPath;
        if (Environment.ProcessPath is { } processPath && Path.GetFileName(processPath).Equals(exe, StringComparison.OrdinalIgnoreCase))
            return processPath;

        // <dotnet root>/shared/Microsoft.NETCore.App/<version>/  →  <dotnet root>/dotnet(.exe)
        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var candidate = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", "..", exe));
        return File.Exists(candidate) ? candidate : "dotnet";
    }

    /// <summary>Reads a stream to the end but only keeps the first N characters.</summary>
    private sealed class CappedReader(StreamReader reader, int maxChars)
    {
        private readonly StringBuilder _buffer = new();
        public bool Exceeded { get; private set; }

        public async Task<string> ReadAllAsync()
        {
            var chunk = new char[4096];
            int read;
            while ((read = await reader.ReadAsync(chunk, 0, chunk.Length)) > 0)
            {
                var room = maxChars - _buffer.Length;
                if (room > 0) _buffer.Append(chunk, 0, Math.Min(room, read));
                if (read > room) Exceeded = true;
            }
            return _buffer.ToString();
        }
    }
}
