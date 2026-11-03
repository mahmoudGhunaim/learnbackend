using LearnBackend.Api.Content;
using LearnBackend.Api.Contracts;
using LearnBackend.Api.Data;
using LearnBackend.Api.Data.Entities;
using LearnBackend.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LearnBackend.Api.Endpoints;

/// <summary>
/// Minimal API style: endpoints are plain methods mapped with MapGet/MapPost.
/// Compare with Controllers/GlossaryController.cs, which does the same kind of work using a controller class.
/// The parameters are resolved automatically:
///   string id              → from the route "{id}"
///   QuizSubmission body    → from the JSON request body
///   ContentStore, AppDbContext... → from Dependency Injection
///   CancellationToken      → cancelled if the browser disconnects
/// </summary>
public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Lessons");

        group.MapGet("/curriculum", GetCurriculum);
        group.MapGet("/lessons/{id}", GetLesson);
        group.MapPost("/lessons/{id}/quiz", SubmitQuiz);

        return app;
    }

    private static async Task<Ok<CurriculumDto>> GetCurriculum(
        ContentStore content, AppDbContext db, ProgressService progress, CancellationToken ct)
    {
        var snapshot = content.Current;
        var statuses = await progress.GetPhaseStatusesAsync(ct);
        var lessonRows = await db.LessonProgress.AsNoTracking().ToDictionaryAsync(l => l.LessonId, ct);
        var solved = (await db.ExerciseProgress.AsNoTracking().Where(e => e.SolvedAt != null).Select(e => e.ExerciseId).ToListAsync(ct)).ToHashSet();

        var phases = snapshot.Curriculum.Phases.OrderBy(p => p.Id).Select(phase => new PhaseDto(
            phase.Id,
            phase.Slug,
            phase.Title,
            phase.Description,
            statuses.First(s => s.Id == phase.Id),
            snapshot.LessonsInPhase(phase.Id).Select(lesson =>
            {
                lessonRows.TryGetValue(lesson.Id, out var row);
                return new LessonSummaryDto(lesson.Id, lesson.Order, lesson.Title, lesson.Summary, lesson.Minutes,
                    row?.CompletedAt is not null, row?.QuizBestScore ?? 0,
                    ExerciseSummaries(snapshot, lesson.Exercises, solved));
            }).ToList(),
            phase.Project is null ? null : ExerciseSummaries(snapshot, [phase.Project], solved).FirstOrDefault()
        )).ToList();

        return TypedResults.Ok(new CurriculumDto(phases));
    }

    private static async Task<Results<Ok<LessonDto>, NotFound, ProblemHttpResult>> GetLesson(
        string id, ContentStore content, AppDbContext db, ProgressService progress, CancellationToken ct)
    {
        var snapshot = content.Current;
        if (!snapshot.Lessons.TryGetValue(id, out var lesson)) return TypedResults.NotFound();
        if (!await progress.IsPhaseUnlockedAsync(lesson.Phase, ct)) return PhaseLocked(lesson.Phase);

        var row = await db.LessonProgress.FirstOrDefaultAsync(l => l.LessonId == id, ct);
        if (row is null)
        {
            row = new LessonProgress { LessonId = id };
            db.LessonProgress.Add(row);
        }
        row.LastVisitedAt = progress.UtcNow;
        await db.SaveChangesAsync(ct);

        var solved = (await db.ExerciseProgress.Where(e => e.SolvedAt != null).Select(e => e.ExerciseId).ToListAsync(ct)).ToHashSet();
        var phaseLessons = snapshot.LessonsInPhase(lesson.Phase).ToList();
        var index = phaseLessons.FindIndex(l => l.Id == id);

        return TypedResults.Ok(new LessonDto(
            lesson.Id, lesson.Phase, lesson.Order, lesson.Title, lesson.Summary, lesson.Minutes, lesson.Terms,
            lesson.Sections,
            lesson.Quiz.Select(q => new QuizQuestionDto(q.Question, q.Options, q.Code)).ToList(),
            ExerciseSummaries(snapshot, lesson.Exercises, solved),
            row.CompletedAt is not null,
            row.QuizBestScore,
            index > 0 ? phaseLessons[index - 1].Id : null,
            index < phaseLessons.Count - 1 ? phaseLessons[index + 1].Id : null));
    }

    private static async Task<Results<Ok<QuizResultDto>, NotFound, ValidationProblem>> SubmitQuiz(
        string id, QuizSubmission submission, ContentStore content, AppDbContext db, ProgressService progress, CancellationToken ct)
    {
        if (!content.Current.Lessons.TryGetValue(id, out var lesson)) return TypedResults.NotFound();
        if (submission.Answers.Length != lesson.Quiz.Count)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["answers"] = [$"Expected {lesson.Quiz.Count} answers but got {submission.Answers.Length}."],
            });

        var questions = lesson.Quiz.Select((q, i) =>
            new QuizQuestionResult(submission.Answers[i], q.Answer, submission.Answers[i] == q.Answer, q.Explanation)).ToList();
        var score = questions.Count(q => q.IsCorrect);
        var passed = score >= Math.Min(progress.PassingQuizScore, lesson.Quiz.Count);

        var row = await db.LessonProgress.FirstOrDefaultAsync(l => l.LessonId == id, ct);
        if (row is null)
        {
            row = new LessonProgress { LessonId = id, LastVisitedAt = progress.UtcNow };
            db.LessonProgress.Add(row);
        }
        row.QuizAttempts++;
        row.QuizTotal = lesson.Quiz.Count;
        row.QuizBestScore = Math.Max(row.QuizBestScore, score);
        if (passed) row.CompletedAt ??= progress.UtcNow;
        await db.SaveChangesAsync(ct);
        await progress.RecordActivityAsync(ct);

        return TypedResults.Ok(new QuizResultDto(score, lesson.Quiz.Count, passed, row.CompletedAt is not null, questions));
    }

    internal static List<ExerciseSummaryDto> ExerciseSummaries(ContentSnapshot snapshot, IEnumerable<string> ids, HashSet<string> solved) =>
        ids.Where(snapshot.Exercises.ContainsKey)
           .Select(x => snapshot.Exercises[x])
           .Select(e => new ExerciseSummaryDto(e.Id, e.Title, e.Kind, e.Difficulty, solved.Contains(e.Id)))
           .ToList();

    internal static ProblemHttpResult PhaseLocked(int phase) => TypedResults.Problem(
        title: "Phase locked",
        detail: $"المرحلة {phase} مقفلة: أكمل دروس المرحلة السابقة ومشروعها أولاً. (Phase {phase} is locked: finish the previous phase and its project first.)",
        statusCode: StatusCodes.Status403Forbidden);
}
