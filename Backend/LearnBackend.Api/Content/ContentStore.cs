using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace LearnBackend.Api.Content;

public sealed class ContentOptions
{
    public const string SectionName = "Content";

    /// <summary>Folder with curriculum.json, lessons/, exercises/... Relative paths are resolved from the API project folder.</summary>
    public string RootPath { get; set; } = "../../content";

    /// <summary>Repository root, used by the "See it in this app" source viewer.</summary>
    public string RepoRoot { get; set; } = "../..";
}

/// <summary>An immutable, fully-loaded copy of all course content.</summary>
public sealed class ContentSnapshot
{
    public required Curriculum Curriculum { get; init; }
    public required IReadOnlyDictionary<string, Lesson> Lessons { get; init; }
    public required IReadOnlyDictionary<string, ExerciseDefinition> Exercises { get; init; }
    public required IReadOnlyList<GlossaryTerm> Glossary { get; init; }
    public required IReadOnlyDictionary<string, GlossaryTerm> GlossaryById { get; init; }
    public required IReadOnlyDictionary<string, CompilerErrorInfo> CompilerErrors { get; init; }

    public IEnumerable<Lesson> LessonsInPhase(int phase) =>
        Lessons.Values.Where(l => l.Phase == phase).OrderBy(l => l.Order);
}

/// <summary>
/// Loads the JSON/C# content files from disk and keeps them in memory.
/// It watches the folder: when you edit a lesson file, the next request sees the change
/// (no restart needed). Singleton lifetime: one shared cache for the whole app.
/// </summary>
public sealed class ContentStore : ICompilerErrorCatalog, IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _root;
    private readonly PhysicalFileProvider _files;
    private readonly IDisposable _watch;
    private readonly ILogger<ContentStore> _logger;
    private readonly Lock _lock = new();
    private ContentSnapshot? _snapshot;
    private volatile bool _stale = true;

    public ContentStore(IOptions<ContentOptions> options, IHostEnvironment env, ILogger<ContentStore> logger)
    {
        _logger = logger;
        _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.RootPath));
        if (!Directory.Exists(_root))
            throw new DirectoryNotFoundException($"Content folder not found: {_root}. Check Content:RootPath in appsettings.json.");

        _files = new PhysicalFileProvider(_root);
        // ChangeToken.OnChange re-subscribes automatically after each change notification.
        _watch = ChangeToken.OnChange(() => _files.Watch("**/*"), () => _stale = true);
    }

    public string RootPath => _root;

    public CompilerErrorInfo? Find(string diagnosticId) =>
        Current.CompilerErrors.TryGetValue(diagnosticId, out var info) ? info : null;

    public ContentSnapshot Current
    {
        get
        {
            if (!_stale && _snapshot is not null) return _snapshot;
            lock (_lock)
            {
                if (_stale || _snapshot is null)
                {
                    _stale = false;
                    _snapshot = Load();
                    _logger.LogInformation("Content loaded: {Lessons} lessons, {Exercises} exercises, {Terms} glossary terms",
                        _snapshot.Lessons.Count, _snapshot.Exercises.Count, _snapshot.Glossary.Count);
                }
                return _snapshot;
            }
        }
    }

    private ContentSnapshot Load()
    {
        var curriculum = Read<Curriculum>("curriculum.json");
        var lessonFiles = Directory.EnumerateFiles(Path.Combine(_root, "lessons"), "*.json", SearchOption.AllDirectories)
            .Select(f => Read<Lesson>(Path.GetRelativePath(_root, f)))
            .ToDictionary(l => l.Id);

        var lessons = new Dictionary<string, Lesson>();
        foreach (var phase in curriculum.Phases)
        {
            for (var i = 0; i < phase.Lessons.Count; i++)
            {
                if (!lessonFiles.TryGetValue(phase.Lessons[i], out var lesson))
                    throw new InvalidDataException($"curriculum.json references unknown lesson '{phase.Lessons[i]}'.");
                lessons[lesson.Id] = lesson with { Phase = phase.Id, Order = i + 1 };
            }
        }

        var exercises = new Dictionary<string, ExerciseDefinition>();
        foreach (var dir in Directory.EnumerateDirectories(Path.Combine(_root, "exercises")))
        {
            var def = Read<ExerciseDefinition>(Path.Combine(Path.GetRelativePath(_root, dir), "exercise.json"));
            var owner = lessons.Values.FirstOrDefault(l => l.Exercises.Contains(def.Id));
            var projectPhase = curriculum.Phases.FirstOrDefault(p => p.Project == def.Id);
            exercises[def.Id] = def with
            {
                LessonId = owner?.Id ?? "",
                Phase = owner?.Phase ?? projectPhase?.Id ?? 0,
                StarterCode = File.ReadAllText(Path.Combine(dir, "starter.cs")),
                SolutionCode = File.ReadAllText(Path.Combine(dir, "solution.cs")),
                TestsCode = File.ReadAllText(Path.Combine(dir, "tests.cs")),
                WrongCode = File.Exists(Path.Combine(dir, "wrong.cs")) ? File.ReadAllText(Path.Combine(dir, "wrong.cs")) : null,
            };
        }

        var glossary = Read<List<GlossaryTerm>>("glossary.json").OrderBy(t => t.En, StringComparer.OrdinalIgnoreCase).ToList();
        var errors = Read<Dictionary<string, CompilerErrorInfo>>("compiler-errors.json");

        return new ContentSnapshot
        {
            Curriculum = curriculum,
            Lessons = lessons,
            Exercises = exercises,
            Glossary = glossary,
            GlossaryById = glossary.ToDictionary(t => t.Id),
            CompilerErrors = errors,
        };
    }

    private T Read<T>(string relativePath)
    {
        var path = Path.Combine(_root, relativePath);
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, JsonOptions)
                   ?? throw new InvalidDataException($"{relativePath} is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON in {relativePath}: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _watch.Dispose();
        _files.Dispose();
    }
}
