# LearnBackend: تعلّم ASP.NET Core من الصفر

An interactive, bilingual (Arabic / English) platform for learning ASP.NET Core. It is itself an
ASP.NET Core + React app, so the codebase is a live example: every lesson points to the exact
file and line in this repository where a concept is used.

## Run it locally

### 1. Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| .NET SDK | 10.0 (LTS) | `dotnet --list-sdks` |
| Node.js | 20 or newer | `node --version` |

Download the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0.

### 2. First-time setup (from the repository root)

```bash
dotnet tool restore                 # installs dotnet-ef (local tool, see dotnet-tools.json)
dotnet restore                      # downloads NuGet packages
npm install --prefix Frontend       # downloads npm packages
```

### 3. Development mode (two terminals, hot reload on both sides)

```bash
# Terminal 1: the API on http://localhost:5064
dotnet watch --project Backend/LearnBackend.Api

# Terminal 2: the React app on http://localhost:5173 (proxies /api to the API)
npm run dev --prefix Frontend
```

Open **http://localhost:5173**.

The SQLite database (`Backend/LearnBackend.Api/learn.db`) is created and migrated automatically
on first start. Delete it to reset your progress.

### 4. Single-server mode (like production)

```bash
npm run build --prefix Frontend     # builds React into Backend/LearnBackend.Api/wwwroot
dotnet run --project Backend/LearnBackend.Api
```

Open **http://localhost:5064**.

### 5. Run the tests

```bash
dotnet test
```

This verifies, for **every** exercise, that the reference solution passes all hidden tests, the starter
code compiles but fails, and a deliberately wrong solution is caught. It also checks sandbox security
(blocked APIs, infinite loops, memory bombs), content integrity (every glossary link, every
"See it in this app" reference, every runnable example compiles) and the HTTP API end to end.

## Useful settings

| Setting | Where | Default |
|---------|-------|---------|
| Unlock every phase | `Learning:UnlockAllPhases` in `appsettings.Development.json` | `false` |
| Passing quiz score | `Learning:PassingQuizScore` | `4` (of 5) |
| Code timeout | `Sandbox:TimeoutSeconds` | `5` |
| Memory limit | `Sandbox:MemoryLimitMb` | `256` |

Any setting can be overridden from the command line, e.g.
`dotnet run --project Backend/LearnBackend.Api -- --Learning:UnlockAllPhases=true`.

## Folder structure

```
LearnBackend.slnx                  solution (new .NET 10 XML format)
dotnet-tools.json                  local .NET tools (dotnet-ef)
Backend/
  LearnBackend.Api/                ASP.NET Core Web API
    Program.cs                     services, middleware pipeline, endpoints
    Controllers/                   controller-style endpoints (glossary, flashcards, dashboard)
    Endpoints/                     minimal-API endpoints (lessons, exercises, playground, source viewer)
    CodeRunner/                    Roslyn compiler, safety analyzer, sandbox process runner
      Harness/TestHarness.cs       mini test framework compiled together with your code
    Content/                       loads /content (lessons, exercises, glossary) with hot reload
    Data/                          EF Core DbContext, entities, migrations (SQLite)
    Services/                      progress rules, spaced repetition, source viewer
  LearnBackend.Sandbox/            isolated worker process that runs compiled learner code
  LearnBackend.Tests/              xUnit: exercises, sandbox security, content, API
content/
  curriculum.json                  phases and lesson order
  glossary.json                    bilingual dictionary
  compiler-errors.json             Arabic explanations of C# compiler errors
  lessons/phase-N/*.json           lesson content
  exercises/<id>/                  exercise.json, starter.cs, solution.cs, wrong.cs, tests.cs
Frontend/                          React + TypeScript + Vite + Tailwind + Monaco
  src/pages/                       dashboard, curriculum, lesson, exercise, glossary, flashcards, playground
  src/components/                  lesson blocks, term cards, code viewers
  src/visuals/                     interactive visualizations
  src/i18n/                        Arabic / English UI strings
```

## How the code checker works

1. **Roslyn** compiles your code, the hidden tests and a tiny test harness into one in-memory assembly.
   Each piece is a separate syntax tree, so compiler errors keep your exact line numbers.
2. A **semantic safety analyzer** rejects dangerous APIs before anything runs: file system, network,
   processes, reflection, native interop, `Environment.Exit`... It works on symbols, not text, so
   aliases like `using F = System.IO.File;` are caught too.
3. The assembly is piped into a **separate worker process** (`LearnBackend.Sandbox`) that runs with a
   GC heap limit, a 5 second wall-clock timeout (the whole process tree is killed), a memory watchdog,
   an output cap and a stripped environment (no secrets inherited).
4. Test results stream back one line at a time, so even if test 3 hangs, tests 1 and 2 still report.

The sandbox is strong enough for a personal learning app. For a multi-tenant public service you would
also run the worker inside a container with no network and a read-only file system.

## Adding content

- Lessons are JSON (`content/lessons`). Inline markup: `**bold**`, `` `code` ``, `[[term-id]]` links a
  glossary term, `[[term-id|custom label]]`.
- "See it in this app" blocks reference a file plus a text anchor (`"find"`), never a hard-coded line
  number. The API resolves the current line at request time, and a test fails if an anchor disappears.
- Content changes are picked up without restarting the API.
