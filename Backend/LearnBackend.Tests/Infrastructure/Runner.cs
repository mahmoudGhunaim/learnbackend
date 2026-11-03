using System.Text.Json;
using LearnBackend.Api.CodeRunner;
using LearnBackend.Api.Content;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LearnBackend.Tests.Infrastructure;

/// <summary>Shared compiler + sandbox instances (building the reference list is slow, so do it once).</summary>
public static class Runner
{
    public static readonly CodeCompiler Compiler = new(new ReferenceCatalog());

    public static readonly SandboxProcessRunner Sandbox = new(
        Options.Create(new SandboxOptions()), NullLogger<SandboxProcessRunner>.Instance);

    public static readonly CodeExecutionService Execution = new(Compiler, Sandbox, new FileErrorCatalog());

    /// <summary>A fake ICompilerErrorCatalog that reads content/compiler-errors.json directly.</summary>
    private sealed class FileErrorCatalog : ICompilerErrorCatalog
    {
        private readonly Dictionary<string, CompilerErrorInfo> _errors =
            JsonSerializer.Deserialize<Dictionary<string, CompilerErrorInfo>>(
                File.ReadAllText(Path.Combine(TestPaths.ContentRoot, "compiler-errors.json")), ContentStore.JsonOptions)!;

        public CompilerErrorInfo? Find(string diagnosticId) => _errors.GetValueOrDefault(diagnosticId);
    }
}
