using LearnBackend.Api.Data.Entities;
using LearnBackend.Api.Services;

namespace LearnBackend.Tests;

public class SpacedRepetitionTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Correct_answers_grow_the_interval_until_mastered()
    {
        var review = new TermReview { TermId = "x" };

        SpacedRepetition.Apply(review, 4, Now);
        Assert.Equal(1, review.IntervalDays);
        SpacedRepetition.Apply(review, 4, Now);
        Assert.Equal(3, review.IntervalDays);
        Assert.False(SpacedRepetition.IsMastered(review));
        SpacedRepetition.Apply(review, 5, Now);

        Assert.True(review.IntervalDays >= 7);
        Assert.True(SpacedRepetition.IsMastered(review));
        Assert.Equal(Now.AddDays(review.IntervalDays), review.DueAt);
    }

    [Fact]
    public void A_miss_resets_the_card_and_lowers_its_ease()
    {
        var review = new TermReview { TermId = "x", Repetitions = 4, IntervalDays = 20, EaseFactor = 2.5 };

        SpacedRepetition.Apply(review, 1, Now);

        Assert.Equal(0, review.Repetitions);
        Assert.Equal(Now.AddMinutes(10), review.DueAt);
        Assert.True(review.EaseFactor < 2.5);
        Assert.Equal(1, review.WrongCount);
    }

    [Fact]
    public void Ease_never_drops_below_the_minimum()
    {
        var review = new TermReview { TermId = "x" };
        for (var i = 0; i < 20; i++) SpacedRepetition.Apply(review, 0, Now);
        Assert.Equal(SpacedRepetition.MinEase, review.EaseFactor);
    }
}
