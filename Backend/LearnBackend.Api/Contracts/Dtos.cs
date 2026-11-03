using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LearnBackend.Api.Content;
using LearnBackend.Api.Services;

namespace LearnBackend.Api.Contracts;

// DTOs (Data Transfer Objects) = the exact shapes the API sends and receives.
// They are deliberately separate from the content models and EF entities. For example,
// QuizQuestionDto has no "Answer" field: the browser must not see the correct answers,
// and ExerciseDto never contains the hidden tests or the solution.

public sealed record CurriculumDto(IReadOnlyList<PhaseDto> Phases);

public sealed record PhaseDto(
    int Id,
    string Slug,
    LocalizedText Title,
    LocalizedText Description,
    PhaseStatus Status,
    IReadOnlyList<LessonSummaryDto> Lessons,
    ExerciseSummaryDto? Project);

public sealed record LessonSummaryDto(
    string Id,
    int Order,
    LocalizedText Title,
    LocalizedText Summary,
    int Minutes,
    bool Completed,
    int QuizBestScore,
    IReadOnlyList<ExerciseSummaryDto> Exercises);

public sealed record ExerciseSummaryDto(string Id, LocalizedText Title, string Kind, string Difficulty, bool Solved);

public sealed record QuizQuestionDto(LocalizedText Question, IReadOnlyList<LocalizedText> Options, string? Code);

public sealed record LessonDto(
    string Id,
    int Phase,
    int Order,
    LocalizedText Title,
    LocalizedText Summary,
    int Minutes,
    IReadOnlyList<string> Terms,
    IReadOnlyList<JsonElement> Sections,
    IReadOnlyList<QuizQuestionDto> Quiz,
    IReadOnlyList<ExerciseSummaryDto> Exercises,
    bool Completed,
    int QuizBestScore,
    string? PreviousLessonId,
    string? NextLessonId);

public sealed class QuizSubmission
{
    [Required]
    public int[] Answers { get; set; } = [];
}

public sealed record QuizQuestionResult(int Selected, int Correct, bool IsCorrect, LocalizedText Explanation);

public sealed record QuizResultDto(int Score, int Total, bool Passed, bool LessonCompleted, IReadOnlyList<QuizQuestionResult> Questions);

public sealed record ExerciseDto(
    string Id,
    LocalizedText Title,
    string Kind,
    string Difficulty,
    int Phase,
    string LessonId,
    LocalizedText Prompt,
    IReadOnlyList<string> Terms,
    string StarterCode,
    string? SavedCode,
    int HintsTotal,
    IReadOnlyList<LocalizedText> RevealedHints,
    bool Solved,
    int Attempts,
    int BestPassed,
    int TotalTests,
    bool CanViewSolution);

public sealed class CodeRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(50_000, ErrorMessage = "Code is too long (max 50,000 characters).")]
    public string Code { get; set; } = "";
}

public sealed record SolutionDto(string Code);

public sealed record DashboardDto(
    IReadOnlyList<PhaseStatus> Phases,
    int Streak,
    int TermsMastered,
    int TermsStudied,
    int TermsTotal,
    int TermsDue,
    int ExercisesSolved,
    int ExercisesTotal,
    int LessonsCompleted,
    int LessonsTotal,
    ContinueDto? Continue,
    IReadOnlyList<WeakTermDto> WeakTerms,
    IReadOnlyList<ActivityDto> Activity);

public sealed record ContinueDto(string LessonId, LocalizedText Title, int Phase);
public sealed record WeakTermDto(string Id, string En, string Ar, int Wrong, int Correct);
public sealed record ActivityDto(DateOnly Date, int Actions);

public sealed record FlashcardDto(
    GlossaryTerm Term,
    bool IsNew,
    int Repetitions,
    double IntervalDays,
    double EaseFactor,
    int CorrectCount,
    int WrongCount);

public sealed class ReviewRequest
{
    /// <summary>0 = forgot … 5 = perfect (SM-2 scale).</summary>
    [Range(0, 5)]
    public int Quality { get; set; }
}

public sealed record ReviewResultDto(string TermId, DateTime DueAt, double IntervalDays, double EaseFactor, bool Mastered);

public sealed record SourceSnippetDto(
    string Path,
    string Language,
    int Line,
    int StartLine,
    IReadOnlyList<string> Lines,
    int TotalLines);
