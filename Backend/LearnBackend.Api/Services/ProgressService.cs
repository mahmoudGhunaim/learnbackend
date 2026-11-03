using LearnBackend.Api.Content;
using LearnBackend.Api.Data;
using LearnBackend.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnBackend.Api.Services;

public sealed class LearningOptions
{
    public const string SectionName = "Learning";

    /// <summary>Set to true in appsettings.Development.json to skip the "finish phase N to unlock N+1" rule.</summary>
    public bool UnlockAllPhases { get; set; }

    /// <summary>Minimum quiz score (out of 5) that completes a lesson.</summary>
    public int PassingQuizScore { get; set; } = 4;
}

public sealed record PhaseStatus(
    int Id,
    bool Unlocked,
    int LessonsCompleted,
    int LessonsTotal,
    int ExercisesSolved,
    int ExercisesTotal,
    bool ProjectSolved,
    int Percent);

/// <summary>
/// Business rules about progress: what is unlocked, percentages, the streak.
/// Scoped lifetime because it depends on AppDbContext, which is Scoped.
/// (A Singleton must never depend on a Scoped service: the "captive dependency" problem.)
/// </summary>
public sealed class ProgressService(AppDbContext db, ContentStore content, IOptions<LearningOptions> options, TimeProvider clock)
{
    public DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    public async Task RecordActivityAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var day = await db.ActivityDays.FindAsync([today], ct);
        if (day is null) db.ActivityDays.Add(new ActivityDay { Date = today, Actions = 1 });
        else day.Actions++;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PhaseStatus>> GetPhaseStatusesAsync(CancellationToken ct = default)
    {
        var snapshot = content.Current;
        var completedLessons = await db.LessonProgress.Where(l => l.CompletedAt != null).Select(l => l.LessonId).ToListAsync(ct);
        var solvedExercises = await db.ExerciseProgress.Where(e => e.SolvedAt != null).Select(e => e.ExerciseId).ToListAsync(ct);
        var completed = completedLessons.ToHashSet();
        var solved = solvedExercises.ToHashSet();

        var result = new List<PhaseStatus>();
        var previousComplete = true;
        foreach (var phase in snapshot.Curriculum.Phases.OrderBy(p => p.Id))
        {
            var exerciseIds = phase.Lessons
                .SelectMany(id => snapshot.Lessons.TryGetValue(id, out var l) ? l.Exercises : [])
                .ToList();
            var lessonsDone = phase.Lessons.Count(completed.Contains);
            var exercisesDone = exerciseIds.Count(solved.Contains);
            var projectDone = phase.Project is not null && solved.Contains(phase.Project);

            var totalItems = phase.Lessons.Count + exerciseIds.Count + (phase.Project is null ? 0 : 1);
            var doneItems = lessonsDone + exercisesDone + (projectDone ? 1 : 0);
            var percent = totalItems == 0 ? 0 : (int)Math.Round(100.0 * doneItems / totalItems);

            var unlocked = options.Value.UnlockAllPhases || phase.Id == 0 || previousComplete;
            result.Add(new PhaseStatus(phase.Id, unlocked && phase.Lessons.Count > 0, lessonsDone, phase.Lessons.Count,
                exercisesDone, exerciseIds.Count, projectDone, percent));

            previousComplete = phase.Lessons.Count > 0
                               && lessonsDone == phase.Lessons.Count
                               && (phase.Project is null || projectDone);
        }
        return result;
    }

    public async Task<bool> IsPhaseUnlockedAsync(int phaseId, CancellationToken ct = default) =>
        (await GetPhaseStatusesAsync(ct)).FirstOrDefault(p => p.Id == phaseId)?.Unlocked ?? false;

    /// <summary>Consecutive days with activity, ending today (or yesterday, so the streak survives until you study today).</summary>
    public async Task<int> GetStreakAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var days = await db.ActivityDays
            .Where(d => d.Date > today.AddDays(-400))
            .OrderByDescending(d => d.Date)
            .Select(d => d.Date)
            .ToListAsync(ct);

        var set = days.ToHashSet();
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (set.Contains(cursor)) { streak++; cursor = cursor.AddDays(-1); }
        return streak;
    }

    public int PassingQuizScore => options.Value.PassingQuizScore;
}
