// =====================================================================
// LearnBackend.Sandbox — عامل العزل (Sandbox Worker)
//
// The API never runs your code inside its own process. It compiles your
// code with Roslyn, then starts THIS small program as a separate process
// and pipes the compiled assembly bytes into its standard input (stdin).
//
// Why a separate process?  .NET Core cannot safely abort a running thread,
// so an infinite loop inside the API would burn a CPU forever. A separate
// process can always be killed, and the OS reclaims all of its memory.
// The API side (CodeRunner/SandboxProcessRunner.cs) enforces:
//   - a hard wall-clock timeout (kills the process tree)
//   - a GC heap limit (DOTNET_GCHeapHardLimit) + a memory watchdog
//   - a cap on how much output it will read
// =====================================================================
using System.Globalization;
using System.Reflection;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

byte[] assemblyBytes;
using (var stdin = Console.OpenStandardInput())
using (var buffer = new MemoryStream())
{
    stdin.CopyTo(buffer);
    assemblyBytes = buffer.ToArray();
}

if (assemblyBytes.Length == 0)
{
    Console.Error.WriteLine("Sandbox: no assembly received on stdin.");
    return 2;
}

var assembly = Assembly.Load(assemblyBytes);
var entryPoint = assembly.EntryPoint;
if (entryPoint is null)
{
    Console.Error.WriteLine("Sandbox: the compiled code has no entry point (Main).");
    return 2;
}

try
{
    object?[]? parameters = entryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()];
    var result = entryPoint.Invoke(null, parameters);
    if (result is Task task) task.GetAwaiter().GetResult();
    Console.Out.Flush();
    return result is int exitCode ? exitCode : 0;
}
catch (TargetInvocationException ex) when (ex.InnerException is not null)
{
    Console.Out.Flush();
    Console.Error.WriteLine($"{ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
    Console.Error.WriteLine(ex.InnerException.StackTrace);
    return 3;
}
