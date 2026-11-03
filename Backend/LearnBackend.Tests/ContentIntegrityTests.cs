using System.Text.Json;
using System.Text.RegularExpressions;
using LearnBackend.Api.Content;
using LearnBackend.Api.Services;
using LearnBackend.Tests.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LearnBackend.Tests;

/// <summary>
/// Catches broken content before a learner does: dangling glossary links, "See it in this app"
/// anchors that no longer exist, quizzes with invalid answers, runnable examples that don't compile.
/// </summary>
public partial class ContentIntegrityTests
{
    private static readonly string[] KnownVisuals =
        ["stack-heap", "linq-pipeline", "async-timeline", "build-pipeline", "project-explorer", "config-layers"];

    private static readonly string[] KnownBlockTypes =
        ["text", "heading", "analogy", "callout", "code", "compare", "annotated", "source", "mistakes", "visual", "table"];

    private static readonly Lazy<ContentStore> Store = new(() =>
        new ContentStore(Options.Create(new ContentOptions { RootPath = TestPaths.ContentRoot }),
            new FakeHostEnvironment(TestPaths.ApiProject), NullLogger<ContentStore>.Instance));

    private static ContentSnapshot Content => Store.Value.Current;

    [GeneratedRegex(@"\[\[([a-z0-9-]+)(?:\|[^\]]+)?\]\]")]
    private static partial Regex TermLink();

    [Fact]
    public void Content_loads_and_phases_0_and_1_are_complete()
    {
        Assert.True(Content.LessonsInPhase(0).Count() >= 8);
        Assert.True(Content.LessonsInPhase(1).Count() >= 6);
        foreach (var phase in Content.Curriculum.Phases.Where(p => p.Lessons.Count > 0))
        {
            Assert.NotNull(phase.Project);
            Assert.True(Content.Exercises.TryGetValue(phase.Project!, out var project), $"Missing project {phase.Project}");
            Assert.Equal("project", project.Kind);
        }
    }

    [Fact]
    public void Every_term_link_points_to_a_glossary_entry()
    {
        var problems = new List<string>();
        foreach (var lesson in Content.Lessons.Values)
        {
            var json = JsonSerializer.Serialize(lesson.Sections) + JsonSerializer.Serialize(lesson.Quiz);
            problems.AddRange(Missing(json).Select(t => $"{lesson.Id}: [[{t}]]"));
            problems.AddRange(lesson.Terms.Where(t => !Content.GlossaryById.ContainsKey(t)).Select(t => $"{lesson.Id} terms: {t}"));
        }
        foreach (var ex in Content.Exercises.Values)
        {
            var json = JsonSerializer.Serialize(ex.Prompt) + JsonSerializer.Serialize(ex.Hints);
            problems.AddRange(Missing(json).Select(t => $"{ex.Id}: [[{t}]]"));
            problems.AddRange((ex.Terms ?? []).Where(t => !Content.GlossaryById.ContainsKey(t)).Select(t => $"{ex.Id} terms: {t}"));
        }
        Assert.Empty(problems);

        IEnumerable<string> Missing(string json) =>
            TermLink().Matches(json).Select(m => m.Groups[1].Value).Where(id => !Content.GlossaryById.ContainsKey(id)).Distinct();
    }

    [Fact]
    public void Glossary_entries_are_complete_and_cross_linked()
    {
        Assert.True(Content.Glossary.Count >= 100);
        foreach (var term in Content.Glossary)
        {
            Assert.False(string.IsNullOrWhiteSpace(term.Ar), term.Id);
            Assert.False(string.IsNullOrWhiteSpace(term.Definition.Ar), term.Id);
            Assert.False(string.IsNullOrWhiteSpace(term.Definition.En), term.Id);
            Assert.All(term.Related, r => Assert.True(Content.GlossaryById.ContainsKey(r), $"{term.Id} → unknown related '{r}'"));
            Assert.True(term.Lesson is null || Content.Lessons.ContainsKey(term.Lesson), $"{term.Id} → unknown lesson '{term.Lesson}'");
        }
    }

    [Fact]
    public void Every_lesson_has_a_valid_quiz_exercises_and_known_blocks()
    {
        foreach (var lesson in Content.Lessons.Values)
        {
            Assert.Equal(5, lesson.Quiz.Count);
            Assert.All(lesson.Quiz, q => Assert.InRange(q.Answer, 0, q.Options.Count - 1));
            Assert.NotEmpty(lesson.Exercises);
            Assert.All(lesson.Exercises, id => Assert.True(Content.Exercises.ContainsKey(id), $"{lesson.Id} → unknown exercise {id}"));

            foreach (var block in lesson.Sections)
            {
                var type = block.GetProperty("type").GetString();
                Assert.Contains(type, KnownBlockTypes);
                if (type == "visual")
                    Assert.Contains(block.GetProperty("name").GetString(), KnownVisuals);
            }

            // Every lesson must point at least once to this app's own code.
            Assert.Contains(lesson.Sections, b => b.GetProperty("type").GetString() == "source");
        }
    }

    [Fact]
    public void Every_exercise_belongs_to_a_lesson_or_is_a_phase_project_and_has_three_hints()
    {
        var projects = Content.Curriculum.Phases.Select(p => p.Project).Where(p => p is not null).ToHashSet();
        foreach (var ex in Content.Exercises.Values)
        {
            Assert.True(ex.LessonId != "" || projects.Contains(ex.Id), $"{ex.Id} is not used by any lesson or phase");
            Assert.Equal(3, ex.Hints.Count);
            Assert.NotNull(ex.WrongCode);
        }
    }

    [Fact]
    public void Every_see_it_in_this_app_reference_resolves()
    {
        var sources = new SourceFileService(
            Options.Create(new ContentOptions { RepoRoot = TestPaths.RepoRoot }), new FakeHostEnvironment(TestPaths.ApiProject));

        var broken = new List<string>();
        foreach (var lesson in Content.Lessons.Values)
        foreach (var block in lesson.Sections.Where(b => b.GetProperty("type").GetString() == "source"))
        {
            var path = block.GetProperty("path").GetString()!;
            var find = block.GetProperty("find").GetString();
            if (sources.GetSnippet(path, find) is null) broken.Add($"{lesson.Id}: {path} → \"{find}\"");
        }
        Assert.Empty(broken);
    }

    [Fact]
    public void Source_viewer_never_serves_hidden_files_or_escapes_the_repo()
    {
        var sources = new SourceFileService(
            Options.Create(new ContentOptions { RepoRoot = TestPaths.RepoRoot }), new FakeHostEnvironment(TestPaths.ApiProject));

        Assert.Null(sources.GetSnippet("content/exercises/p0-ex-price/solution.cs", null));
        Assert.Null(sources.GetSnippet("content/exercises/p0-ex-price/tests.cs", null));
        Assert.Null(sources.GetSnippet("Backend/../../Windows/win.ini", null));
        Assert.Null(sources.GetSnippet("Backend/LearnBackend.Api/bin/Debug/net10.0/appsettings.json", null));
        Assert.NotNull(sources.GetSnippet("content/exercises/p0-ex-price/starter.cs", null));
    }

    [Fact]
    public void Every_runnable_example_compiles_in_the_sandbox()
    {
        var failures = new List<string>();
        foreach (var lesson in Content.Lessons.Values)
        foreach (var block in lesson.Sections.Where(b => b.GetProperty("type").GetString() == "code"
                                                       && b.TryGetProperty("runnable", out var r) && r.GetBoolean()))
        {
            var compiled = Runner.Compiler.Compile(block.GetProperty("code").GetString()!, testsCode: null);
            if (!compiled.Success)
                failures.Add($"{lesson.Id}: " + string.Join("; ",
                    compiled.UserDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.ToString())
                        .Concat(compiled.Violations.Select(v => "blocked " + v.Api))));
        }
        Assert.Empty(failures);
    }

    private sealed class FakeHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "LearnBackend.Api";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
