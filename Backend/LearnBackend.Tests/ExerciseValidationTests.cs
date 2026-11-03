using LearnBackend.Api.CodeRunner;
using LearnBackend.Tests.Infrastructure;
using Xunit.Abstractions;

namespace LearnBackend.Tests;

/// <summary>
/// For EVERY exercise folder in /content/exercises:
///   solution.cs → compiles and passes all hidden tests
///   starter.cs  → compiles (so the learner starts from valid code) but fails the tests
///   wrong.cs    → a realistic mistake that must be caught by at least one test
/// </summary>
public class ExerciseValidationTests(ITestOutputHelper output)
{
    public static TheoryData<string> ExerciseIds()
    {
        var data = new TheoryData<string>();
        foreach (var dir in Directory.EnumerateDirectories(Path.Combine(TestPaths.ContentRoot, "exercises")).Order())
            data.Add(Path.GetFileName(dir));
        return data;
    }

    private static string Read(string id, string file) =>
        File.ReadAllText(Path.Combine(TestPaths.ContentRoot, "exercises", id, file));

    [Theory]
    [MemberData(nameof(ExerciseIds))]
    public async Task Solution_passes_all_tests(string id)
    {
        var result = await Runner.Execution.RunTestsAsync(Read(id, "solution.cs"), Read(id, "tests.cs"), CancellationToken.None);
        Report(result);

        Assert.Equal(RunOutcome.Passed, result.Outcome);
        Assert.True(result.TotalCount >= 5, $"{id} should have at least 5 tests, has {result.TotalCount}");
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == "warning");
    }

    [Theory]
    [MemberData(nameof(ExerciseIds))]
    public async Task Starter_compiles_but_fails(string id)
    {
        var result = await Runner.Execution.RunTestsAsync(Read(id, "starter.cs"), Read(id, "tests.cs"), CancellationToken.None);
        Report(result);

        Assert.Equal(RunOutcome.Failed, result.Outcome);
        Assert.True(result.PassedCount < result.TotalCount);
    }

    [Theory]
    [MemberData(nameof(ExerciseIds))]
    public async Task Wrong_solution_is_caught(string id)
    {
        var result = await Runner.Execution.RunTestsAsync(Read(id, "wrong.cs"), Read(id, "tests.cs"), CancellationToken.None);
        Report(result);

        Assert.NotEqual(RunOutcome.Passed, result.Outcome);
    }

    private void Report(SubmissionResult result)
    {
        output.WriteLine($"Outcome: {result.Outcome}  ({result.PassedCount}/{result.TotalCount}) in {result.ElapsedMs} ms");
        foreach (var d in result.Diagnostics)
            output.WriteLine($"  [{d.Source}] {d.Id} line {d.Line}: {d.Message}");
        foreach (var t in result.Tests)
            output.WriteLine($"  {t.Status,-8} {t.Name} {t.Message} {(t.Expected is null ? "" : $"expected={t.Expected} actual={t.Actual}")}");
        if (result.Error is not null) output.WriteLine("  error: " + result.Error);
    }
}
