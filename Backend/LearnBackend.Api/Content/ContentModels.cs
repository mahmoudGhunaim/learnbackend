using System.Text.Json;

namespace LearnBackend.Api.Content;

// Records are perfect for content: immutable data with value equality.
// These map 1:1 to the JSON files in /content (System.Text.Json, camelCase).

/// <summary>A text in Arabic with an optional English version.</summary>
public sealed record LocalizedText(string Ar, string? En = null);

public sealed record Curriculum(IReadOnlyList<PhaseDefinition> Phases);

public sealed record PhaseDefinition(
    int Id,
    string Slug,
    LocalizedText Title,
    LocalizedText Description,
    IReadOnlyList<string> Lessons,
    string? Project);

public sealed record QuizQuestion(
    LocalizedText Question,
    IReadOnlyList<LocalizedText> Options,
    int Answer,
    LocalizedText Explanation,
    string? Code = null);

public sealed record Lesson(
    string Id,
    LocalizedText Title,
    LocalizedText Summary,
    int Minutes,
    IReadOnlyList<string> Terms,
    IReadOnlyList<string> Exercises,
    IReadOnlyList<JsonElement> Sections,   // passed through to the React renderer as-is
    IReadOnlyList<QuizQuestion> Quiz)
{
    /// <summary>Filled by the ContentStore from curriculum.json, not from the lesson file.</summary>
    public int Phase { get; init; }
    public int Order { get; init; }
}

public sealed record ExerciseDefinition(
    string Id,
    LocalizedText Title,
    string Kind,                 // "exercise" | "project"
    string Difficulty,           // "easy" | "medium" | "hard"
    LocalizedText Prompt,
    IReadOnlyList<LocalizedText> Hints,
    IReadOnlyList<string>? Terms = null)
{
    public string LessonId { get; init; } = "";
    public int Phase { get; init; }
    public string StarterCode { get; init; } = "";
    public string SolutionCode { get; init; } = "";
    public string TestsCode { get; init; } = "";
    public string? WrongCode { get; init; }
}

public sealed record GlossaryTerm(
    string Id,
    string En,
    string Ar,
    LocalizedText Definition,
    string? Snippet,
    IReadOnlyList<string> Related,
    string? Lesson,
    string? Category = null);

public sealed record CompilerErrorInfo(string Ar, string? TipAr = null, string? TipEn = null);

/// <summary>
/// Looks up the Arabic explanation of a compiler error code (CS0103...).
/// An interface so the code runner doesn't depend on the whole ContentStore,
/// and tests can pass a simple fake.
/// </summary>
public interface ICompilerErrorCatalog
{
    CompilerErrorInfo? Find(string diagnosticId);
}
