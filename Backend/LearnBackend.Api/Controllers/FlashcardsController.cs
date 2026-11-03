using LearnBackend.Api.Content;
using LearnBackend.Api.Contracts;
using LearnBackend.Api.Data;
using LearnBackend.Api.Data.Entities;
using LearnBackend.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnBackend.Api.Controllers;

[ApiController]
[Route("api/flashcards")]
public class FlashcardsController(AppDbContext db, ContentStore content, ProgressService progress) : ControllerBase
{
    /// <summary>
    /// Builds a study session: first the cards that are due (weakest first = lowest ease factor),
    /// then new terms you haven't studied yet, preferring terms from lessons you've opened.
    /// </summary>
    // GET api/flashcards/session?limit=20&phase=0
    [HttpGet("session")]
    public async Task<ActionResult<IEnumerable<FlashcardDto>>> GetSession(
        [FromQuery] int limit = 20, [FromQuery] int? phase = null, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var snapshot = content.Current;
        var now = progress.UtcNow;

        var allowedTerms = snapshot.Glossary
            .Where(t => phase is null || (t.Lesson is not null && snapshot.Lessons.TryGetValue(t.Lesson, out var l) && l.Phase == phase))
            .ToDictionary(t => t.Id);

        var reviews = await db.TermReviews.AsNoTracking().ToListAsync(ct);
        var reviewed = reviews.ToDictionary(r => r.TermId);

        var due = reviews
            .Where(r => r.DueAt <= now && allowedTerms.ContainsKey(r.TermId))
            .OrderBy(r => r.EaseFactor)
            .ThenBy(r => r.DueAt)
            .Select(r => ToDto(allowedTerms[r.TermId], r));

        var visitedLessons = (await db.LessonProgress.AsNoTracking().Select(l => l.LessonId).ToListAsync(ct)).ToHashSet();
        var fresh = allowedTerms.Values
            .Where(t => !reviewed.ContainsKey(t.Id))
            .OrderByDescending(t => t.Lesson is not null && visitedLessons.Contains(t.Lesson))
            .ThenBy(t => t.Lesson is not null && snapshot.Lessons.TryGetValue(t.Lesson, out var l) ? l.Phase * 100 + l.Order : 9999)
            .Select(t => ToDto(t, null));

        return Ok(due.Concat(fresh).Take(limit).ToList());
    }

    // POST api/flashcards/value-type/review   { "quality": 4 }
    [HttpPost("{termId}/review")]
    public async Task<ActionResult<ReviewResultDto>> Review(string termId, ReviewRequest request, CancellationToken ct)
    {
        // If [Range(0,5)] fails, [ApiController] already returned 400 before we get here.
        if (!content.Current.GlossaryById.ContainsKey(termId)) return NotFound();

        var review = await db.TermReviews.FirstOrDefaultAsync(r => r.TermId == termId, ct);
        if (review is null)
        {
            review = new TermReview { TermId = termId };
            db.TermReviews.Add(review);
        }

        SpacedRepetition.Apply(review, request.Quality, progress.UtcNow);
        await db.SaveChangesAsync(ct);
        await progress.RecordActivityAsync(ct);

        return Ok(new ReviewResultDto(termId, review.DueAt, review.IntervalDays, review.EaseFactor, SpacedRepetition.IsMastered(review)));
    }

    private static FlashcardDto ToDto(GlossaryTerm term, TermReview? r) =>
        new(term, r is null, r?.Repetitions ?? 0, r?.IntervalDays ?? 0, r?.EaseFactor ?? 2.5, r?.CorrectCount ?? 0, r?.WrongCount ?? 0);
}
