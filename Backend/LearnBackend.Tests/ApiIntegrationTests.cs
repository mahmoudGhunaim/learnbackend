using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LearnBackend.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnBackend.Tests;

/// <summary>
/// Runs the real API in memory (WebApplicationFactory) against a throwaway SQLite database.
/// Each test class instance gets a fresh database, so tests don't influence each other.
/// </summary>
public sealed class ApiIntegrationTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"learn-test-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
            builder.UseSetting("Content:RootPath", TestPaths.ContentRoot);
            builder.UseSetting("Content:RepoRoot", TestPaths.RepoRoot);
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Curriculum_unlocks_only_phase_0_at_first()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/api/curriculum");
        var phases = json.GetProperty("phases").EnumerateArray().ToList();

        Assert.Equal(8, phases.Count);
        Assert.True(phases[0].GetProperty("status").GetProperty("unlocked").GetBoolean());
        Assert.False(phases[1].GetProperty("status").GetProperty("unlocked").GetBoolean());
        Assert.Equal(8, phases[0].GetProperty("lessons").GetArrayLength());
    }

    [Fact]
    public async Task Locked_lessons_return_403_problem_details()
    {
        var response = await _client.GetAsync("/api/lessons/p1-runtime");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Lesson_does_not_leak_quiz_answers()
    {
        var raw = await _client.GetStringAsync("/api/lessons/p0-types");

        Assert.DoesNotContain("\"answer\"", raw);
        Assert.Contains("\"quiz\"", raw);
    }

    [Fact]
    public async Task Passing_the_quiz_completes_the_lesson()
    {
        var answers = await CorrectAnswers("p0-types");

        var response = await _client.PostAsJsonAsync("/api/lessons/p0-types/quiz", new { answers });
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(5, result.GetProperty("score").GetInt32());
        Assert.True(result.GetProperty("lessonCompleted").GetBoolean());
    }

    [Fact]
    public async Task Quiz_with_wrong_number_of_answers_is_a_400()
    {
        var response = await _client.PostAsJsonAsync("/api/lessons/p0-types/quiz", new { answers = new[] { 0 } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submitting_the_solution_solves_the_exercise_and_unlocks_the_solution()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/exercises/p0-ex-price/solution")).StatusCode);

        var solution = await File.ReadAllTextAsync(Path.Combine(TestPaths.ContentRoot, "exercises", "p0-ex-price", "solution.cs"));
        var submit = await _client.PostAsJsonAsync("/api/exercises/p0-ex-price/submit", new { code = solution });
        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Passed", result.GetProperty("outcome").GetString());
        var exercise = await _client.GetFromJsonAsync<JsonElement>("/api/exercises/p0-ex-price");
        Assert.True(exercise.GetProperty("solved").GetBoolean());
        Assert.Equal(solution, exercise.GetProperty("savedCode").GetString());
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/exercises/p0-ex-price/solution")).StatusCode);
    }

    [Fact]
    public async Task Exercise_never_exposes_tests_or_solution()
    {
        var raw = await _client.GetStringAsync("/api/exercises/p0-ex-price");
        Assert.DoesNotContain("LearnHarness", raw);
        Assert.DoesNotContain("MidpointRounding.AwayFromZero);", raw); // only in the solution
    }

    [Fact]
    public async Task Three_hints_unlock_the_solution()
    {
        for (var i = 0; i < 3; i++)
            await _client.PostAsync("/api/exercises/p0-ex-bank-account/hints", null);

        var exercise = await _client.GetFromJsonAsync<JsonElement>("/api/exercises/p0-ex-bank-account");
        Assert.Equal(3, exercise.GetProperty("revealedHints").GetArrayLength());
        Assert.True(exercise.GetProperty("canViewSolution").GetBoolean());
    }

    [Fact]
    public async Task Compile_errors_come_back_with_arabic_explanations_and_line_numbers()
    {
        var submit = await _client.PostAsJsonAsync("/api/exercises/p0-ex-price/submit",
            new { code = "public static class PriceCalculator\n{\n    public static decimal Subtotal(decimal p, int q) => p * q\n}" });
        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("CompileError", result.GetProperty("outcome").GetString());
        var cs1002 = result.GetProperty("diagnostics").EnumerateArray().First(d => d.GetProperty("id").GetString() == "CS1002");
        Assert.Equal(3, cs1002.GetProperty("line").GetInt32());
        Assert.Contains("فاصلة منقوطة", cs1002.GetProperty("messageAr").GetString());
    }

    [Fact]
    public async Task Empty_code_is_rejected_by_validation()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises/p0-ex-price/submit", new { code = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Playground_runs_code_and_blocks_dangerous_code()
    {
        var ok = await (await _client.PostAsJsonAsync("/api/playground/run", new { code = "Console.WriteLine(6 * 7);" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed", ok.GetProperty("outcome").GetString());
        Assert.Equal("42", ok.GetProperty("output").GetString()!.Trim());

        var blocked = await (await _client.PostAsJsonAsync("/api/playground/run", new { code = "System.IO.File.Delete(\"x\");" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Blocked", blocked.GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task Flashcard_review_validates_quality_and_updates_the_dashboard()
    {
        var bad = await _client.PostAsJsonAsync("/api/flashcards/decimal/review", new { quality = 9 });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var good = await _client.PostAsJsonAsync("/api/flashcards/decimal/review", new { quality = 5 });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);

        var dashboard = await _client.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(1, dashboard.GetProperty("termsStudied").GetInt32());
        Assert.Equal(1, dashboard.GetProperty("streak").GetInt32());
    }

    [Fact]
    public async Task Source_viewer_resolves_anchors_to_line_numbers()
    {
        var snippet = await _client.GetFromJsonAsync<JsonElement>(
            "/api/source?path=Backend/LearnBackend.Api/Program.cs&find=" + Uri.EscapeDataString("var app = builder.Build();"));

        var line = snippet.GetProperty("line").GetInt32();
        var lines = File.ReadAllLines(Path.Combine(TestPaths.ApiProject, "Program.cs"));
        Assert.Contains("var app = builder.Build();", lines[line - 1]);

        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.GetAsync("/api/source?path=content/exercises/p0-ex-price/tests.cs")).StatusCode);
    }

    private static async Task<int[]> CorrectAnswers(string lessonId)
    {
        var file = Directory.EnumerateFiles(Path.Combine(TestPaths.ContentRoot, "lessons"), "*.json", SearchOption.AllDirectories)
            .Select(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement)
            .First(l => l.GetProperty("id").GetString() == lessonId);
        return file.GetProperty("quiz").EnumerateArray().Select(q => q.GetProperty("answer").GetInt32()).ToArray();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}
