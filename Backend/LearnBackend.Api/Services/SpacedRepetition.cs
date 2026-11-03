using LearnBackend.Api.Data.Entities;

namespace LearnBackend.Api.Services;

/// <summary>
/// SM-2 spaced repetition (the algorithm behind Anki/SuperMemo), simplified.
/// quality: 0 = forgot completely … 5 = perfect recall. Below 3 counts as a miss.
///
/// Terms you miss come back after 10 minutes and their "ease" drops, so weak terms
/// show up more often; terms you know get exponentially longer intervals (1d, 3d, ~8d, ~20d…).
/// Pure static logic with no database access, which makes it trivial to unit-test.
/// </summary>
public static class SpacedRepetition
{
    public const double MinEase = 1.3;

    public static void Apply(TermReview review, int quality, DateTime now)
    {
        quality = Math.Clamp(quality, 0, 5);

        if (quality < 3)
        {
            review.Repetitions = 0;
            review.IntervalDays = 0;
            review.WrongCount++;
            review.DueAt = now.AddMinutes(10);
        }
        else
        {
            review.Repetitions++;
            review.CorrectCount++;
            review.IntervalDays = review.Repetitions switch
            {
                1 => 1,
                2 => 3,
                _ => Math.Round(review.IntervalDays * review.EaseFactor, 1),
            };
            review.DueAt = now.AddDays(review.IntervalDays);
        }

        // Standard SM-2 ease update: EF' = EF + (0.1 - (5-q)(0.08 + (5-q)0.02))
        review.EaseFactor = Math.Max(MinEase,
            review.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        review.LastReviewedAt = now;
    }

    /// <summary>A term counts as "mastered" once it has survived an interval of a week or more.</summary>
    public static bool IsMastered(TermReview review) => review.IntervalDays >= 7;
}
