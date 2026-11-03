using LearnBackend.Api.Content;
using LearnBackend.Api.Contracts;
using LearnBackend.Api.Data;
using LearnBackend.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(AppDbContext db, ContentStore content, ProgressService progress) : ControllerBase
{
    // GET api/dashboard
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
    {
        var snapshot = content.Current;
        var now = progress.UtcNow;
        var phases = await progress.GetPhaseStatusesAsync(ct);

        var reviews = await db.TermReviews.AsNoTracking().ToListAsync(ct);
        var lessonRows = await db.LessonProgress.AsNoTracking().ToListAsync(ct);
        var solvedCount = await db.ExerciseProgress.CountAsync(e => e.SolvedAt != null, ct);
        var activity = await db.ActivityDays.AsNoTracking()
            .OrderByDescending(d => d.Date).Take(84)
            .Select(d => new ActivityDto(d.Date, d.Actions))
            .ToListAsync(ct);

        // "Continue where you left off": the first unfinished lesson in an unlocked phase.
        var completed = lessonRows.Where(l => l.CompletedAt != null).Select(l => l.LessonId).ToHashSet();
        var next = snapshot.Curriculum.Phases.OrderBy(p => p.Id)
            .Where(p => phases.First(s => s.Id == p.Id).Unlocked)
            .SelectMany(p => snapshot.LessonsInPhase(p.Id))
            .FirstOrDefault(l => !completed.Contains(l.Id));

        var weak = reviews
            .Where(r => r.WrongCount > 0 && snapshot.GlossaryById.ContainsKey(r.TermId))
            .OrderByDescending(r => r.WrongCount - r.CorrectCount * 0.5)
            .Take(6)
            .Select(r => new WeakTermDto(r.TermId, snapshot.GlossaryById[r.TermId].En, snapshot.GlossaryById[r.TermId].Ar, r.WrongCount, r.CorrectCount))
            .ToList();

        return Ok(new DashboardDto(
            phases,
            await progress.GetStreakAsync(ct),
            reviews.Count(SpacedRepetition.IsMastered),
            reviews.Count,
            snapshot.Glossary.Count,
            reviews.Count(r => r.DueAt <= now),
            solvedCount,
            snapshot.Exercises.Count,
            completed.Count,
            snapshot.Lessons.Count,
            next is null ? null : new ContinueDto(next.Id, next.Title, next.Phase),
            weak,
            activity));
    }
}
