using LearnBackend.Api.Content;
using LearnBackend.Api.Contracts;
using Microsoft.Extensions.Options;

namespace LearnBackend.Api.Services;

/// <summary>
/// Powers "See it in this app": returns a snippet of a real file from THIS repository.
/// Lessons reference a file plus a search anchor (text to find) instead of a hard-coded
/// line number, so references stay correct when the code changes.
///
/// Security: only files inside the allow-listed folders/extensions can be read, and
/// "../" path traversal is rejected by comparing full paths.
/// </summary>
public sealed class SourceFileService
{
    private static readonly string[] AllowedRoots =
    [
        "Backend", "Frontend/src", "Frontend/index.html", "Frontend/vite.config.ts", "Frontend/package.json", "content",
        "LearnBackend.slnx", "dotnet-tools.json", "README.md",
    ];
    private static readonly string[] BlockedSegments = ["bin", "obj", "node_modules", ".git"];
    private static readonly string[] HiddenExerciseFiles = ["solution.cs", "tests.cs", "wrong.cs"];
    private static readonly Dictionary<string, string> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "csharp", [".csproj"] = "xml", [".slnx"] = "xml", [".json"] = "json",
        [".ts"] = "typescript", [".tsx"] = "typescript", [".css"] = "css", [".html"] = "html", [".http"] = "http", [".md"] = "markdown",
    };

    private readonly string _repoRoot;

    public SourceFileService(IOptions<ContentOptions> options, IHostEnvironment env)
    {
        _repoRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.RepoRoot));
    }

    public SourceSnippetDto? GetSnippet(string relativePath, string? find, int context = 6)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (!AllowedRoots.Any(r => normalized == r || normalized.StartsWith(r + "/", StringComparison.OrdinalIgnoreCase)))
            return null;
        if (normalized.Split('/').Any(s => BlockedSegments.Contains(s, StringComparer.OrdinalIgnoreCase)))
            return null;
        // Hidden tests and reference solutions must never be served (the solution endpoint has its own rules).
        if (HiddenExerciseFiles.Contains(Path.GetFileName(normalized), StringComparer.OrdinalIgnoreCase))
            return null;
        if (!Languages.TryGetValue(Path.GetExtension(normalized), out var language))
            return null;

        var fullPath = Path.GetFullPath(Path.Combine(_repoRoot, normalized));
        if (!fullPath.StartsWith(_repoRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            return null;

        var lines = File.ReadAllLines(fullPath);
        var line = 1;
        if (!string.IsNullOrEmpty(find))
        {
            var index = Array.FindIndex(lines, l => l.Contains(find, StringComparison.Ordinal));
            if (index < 0) return null;
            line = index + 1;
        }

        context = Math.Clamp(context, 0, 400);
        var start = string.IsNullOrEmpty(find) ? 1 : Math.Max(1, line - context);
        var end = string.IsNullOrEmpty(find) ? Math.Min(lines.Length, 400) : Math.Min(lines.Length, line + context);
        return new SourceSnippetDto(normalized, language, line, start, lines[(start - 1)..end], lines.Length);
    }
}
