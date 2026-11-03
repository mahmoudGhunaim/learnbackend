namespace LearnBackend.Api.Data.Entities;

// Entities = classes that EF Core maps to database tables.
// Convention: a property named "Id" becomes the primary key; each DbSet<T> becomes a table.
// The app is single-user for now; Phase 5 (Security) adds a UserId to each of these.

/// <summary>One row per lesson you have opened.</summary>
public class LessonProgress
{
    public int Id { get; set; }
    public required string LessonId { get; set; }
    public int QuizBestScore { get; set; }
    public int QuizTotal { get; set; }
    public int QuizAttempts { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastVisitedAt { get; set; }
}

/// <summary>Your saved code and status for one exercise or phase project.</summary>
public class ExerciseProgress
{
    public int Id { get; set; }
    public required string ExerciseId { get; set; }
    public string? SavedCode { get; set; }
    public int HintsUsed { get; set; }
    public int Attempts { get; set; }
    public int BestPassed { get; set; }
    public int TotalTests { get; set; }
    public bool SolutionViewed { get; set; }
    public DateTime? SolvedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Spaced-repetition state of one glossary term (SM-2 algorithm).</summary>
public class TermReview
{
    public int Id { get; set; }
    public required string TermId { get; set; }
    public int Repetitions { get; set; }
    public double IntervalDays { get; set; }
    public double EaseFactor { get; set; } = 2.5;
    public DateTime DueAt { get; set; }
    public DateTime LastReviewedAt { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
}

/// <summary>One row per day you did something; used to compute the streak.</summary>
public class ActivityDay
{
    public DateOnly Date { get; set; }
    public int Actions { get; set; }
}
