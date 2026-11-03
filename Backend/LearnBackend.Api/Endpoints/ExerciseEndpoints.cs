using LearnBackend.Api.CodeRunner;
using LearnBackend.Api.Content;
using LearnBackend.Api.Contracts;
using LearnBackend.Api.Data;
using LearnBackend.Api.Data.Entities;
using LearnBackend.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LearnBackend.Api.Endpoints;

/// <summary>Exercises, the code runner and the playground (Minimal APIs).</summary>
public static class ExerciseEndpoints
{
    public static IEndpointRouteBuilder MapExerciseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exercises").WithTags("Exercises");

        group.MapGet("/{id}", GetExercise);
        group.MapPut("/{id}/code", SaveCode);
        group.MapPost("/{id}/hints", RevealHint);
        group.MapPost("/{id}/submit", Submit);
        group.MapGet("/{id}/solution", GetSolution);

        app.MapPost("/api/playground/run", RunPlayground).WithTags("Playground");

        app.MapGet("/api/source", (string path, string? find, int? context, SourceFileService sources) =>
                sources.GetSnippet(path, find, context ?? 6) is { } snippet
                    ? Results.Ok(snippet)
                    : Results.NotFound())
            .WithTags("Source");

        return app;
    }

    private static async Task<Results<Ok<ExerciseDto>, NotFound, ProblemHttpResult>> GetExercise(
        string id, ContentStore content, AppDbContext db, ProgressService progress, CancellationToken ct)
    {
        if (!content.Current.Exercises.TryGetValue(id, out var exercise)) return TypedResults.NotFound();
        if (!await progress.IsPhaseUnlockedAsync(exercise.Phase, ct)) return LessonEndpoints.PhaseLocked(exercise.Phase);

        var row = await db.ExerciseProgress.AsNoTracking().FirstOrDefaultAsync(e => e.ExerciseId == id, ct);
        return TypedResults.Ok(ToDto(exercise, row));
    }

    private static async Task<Results<NoContent, NotFound>> SaveCode(
        string id, CodeRequest request, ContentStore content, AppDbContext db, ProgressService progress, CancellationToken ct)
    {
        if (!content.Current.Exercises.ContainsKey(id)) return TypedResults.NotFound();
        var row = await GetOrCreateAsync(db, id, ct);
        row.SavedCode = request.Code;
        row.UpdatedAt = progress.UtcNow;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ExerciseDto>, NotFound>> RevealHint(
        string id, ContentStore content, AppDbContext db, ProgressService progress, CancellationToken ct)
    {
        if (!content.Current.Exercises.TryGetValue(id, out var exercise)) return TypedResults.NotFound();
        var row = await GetOrCreateAsync(db, id, ct);
        row.HintsUsed = Math.Min(row.HintsUsed + 1, exercise.Hints.Count);
        row.UpdatedAt = progress.UtcNow;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToDto(exercise, row));
    }

    private static async Task<Results<Ok<SubmissionResult>, NotFound, ProblemHttpResult>> Submit(
        string id, CodeRequest request, ContentStore content, AppDbContext db, ProgressService progress,
        CodeExecutionService runner, CancellationToken ct)
    {
        if (!content.Current.Exercises.TryGetValue(id, out var exercise)) return TypedResults.NotFound();
        if (!await progress.IsPhaseUnlockedAsync(exercise.Phase, ct)) return LessonEndpoints.PhaseLocked(exercise.Phase);

        var result = await runner.RunTestsAsync(request.Code, exercise.TestsCode, ct);

        var row = await GetOrCreateAsync(db, id, ct);
        row.SavedCode = request.Code;
        row.Attempts++;
        row.UpdatedAt = progress.UtcNow;
        if (result.TotalCount > 0)
        {
            row.TotalTests = result.TotalCount;
            row.BestPassed = Math.Max(row.BestPassed, result.PassedCount);
        }
        if (result.Outcome == RunOutcome.Passed) row.SolvedAt ??= progress.UtcNow;
        await db.SaveChangesAsync(ct);
        await progress.RecordActivityAsync(ct);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<SolutionDto>, NotFound, ProblemHttpResult>> GetSolution(
        string id, ContentStore content, AppDbContext db, CancellationToken ct)
    {
        if (!content.Current.Exercises.TryGetValue(id, out var exercise)) return TypedResults.NotFound();
        var row = await db.ExerciseProgress.FirstOrDefaultAsync(e => e.ExerciseId == id, ct);
        if (row is null || !CanViewSolution(exercise, row))
            return TypedResults.Problem(
                title: "Solution locked",
                detail: "استخدم التلميحات الثلاثة أولاً، أو حُلّ التمرين. (Use all hints first, or solve the exercise.)",
                statusCode: StatusCodes.Status403Forbidden);

        row.SolutionViewed = true;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new SolutionDto(exercise.SolutionCode));
    }

    private static async Task<Ok<ProgramRunResult>> RunPlayground(CodeRequest request, CodeExecutionService runner, CancellationToken ct) =>
        TypedResults.Ok(await runner.RunProgramAsync(request.Code, ct));

    private static async Task<ExerciseProgress> GetOrCreateAsync(AppDbContext db, string id, CancellationToken ct)
    {
        var row = await db.ExerciseProgress.FirstOrDefaultAsync(e => e.ExerciseId == id, ct);
        if (row is not null) return row;
        row = new ExerciseProgress { ExerciseId = id };
        db.ExerciseProgress.Add(row);
        return row;
    }

    private static bool CanViewSolution(ExerciseDefinition exercise, ExerciseProgress? row) =>
        row is not null && (row.SolvedAt is not null || row.HintsUsed >= exercise.Hints.Count);

    private static ExerciseDto ToDto(ExerciseDefinition e, ExerciseProgress? row) => new(
        e.Id, e.Title, e.Kind, e.Difficulty, e.Phase, e.LessonId, e.Prompt, e.Terms ?? [],
        e.StarterCode, row?.SavedCode, e.Hints.Count,
        e.Hints.Take(row?.HintsUsed ?? 0).ToList(),
        row?.SolvedAt is not null, row?.Attempts ?? 0, row?.BestPassed ?? 0, row?.TotalTests ?? 0,
        CanViewSolution(e, row));
}
