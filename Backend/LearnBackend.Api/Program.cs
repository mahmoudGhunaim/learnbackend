// =====================================================================
// Program.cs — the entry point of the ASP.NET Core application.
// (Top-level statements: no "class Program { static void Main }" needed;
//  the compiler generates it for you.)
//
// It has two halves:
//   1) builder  → register SERVICES in the DI container (what the app can use)
//   2) app      → build the MIDDLEWARE PIPELINE + map ENDPOINTS (how requests flow)
// =====================================================================
using LearnBackend.Api.CodeRunner;
using LearnBackend.Api.Content;
using LearnBackend.Api.Data;
using LearnBackend.Api.Endpoints;
using System.Text.Json.Serialization;
using LearnBackend.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1) Services (Dependency Injection container) ----------

// Options pattern: bind sections of appsettings.json to strongly-typed classes.
builder.Services.Configure<SandboxOptions>(builder.Configuration.GetSection(SandboxOptions.SectionName));
builder.Services.Configure<ContentOptions>(builder.Configuration.GetSection(ContentOptions.SectionName));
builder.Services.Configure<LearningOptions>(builder.Configuration.GetSection(LearningOptions.SectionName));

// EF Core + SQLite. The connection string comes from appsettings.json → "ConnectionStrings:Default".
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Singleton: ONE instance for the whole app lifetime (caches, expensive-to-build objects).
builder.Services.AddSingleton<ContentStore>();
// The same ContentStore instance also serves as the ICompilerErrorCatalog implementation.
builder.Services.AddSingleton<ICompilerErrorCatalog>(sp => sp.GetRequiredService<ContentStore>());
builder.Services.AddSingleton<ReferenceCatalog>();
builder.Services.AddSingleton<CodeCompiler>();
builder.Services.AddSingleton<SandboxProcessRunner>();
builder.Services.AddSingleton<SourceFileService>();
builder.Services.AddSingleton(TimeProvider.System);

// Scoped: one instance PER HTTP REQUEST (anything that uses the DbContext).
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<CodeExecutionService>();

// Background service: warms up Roslyn at startup so your first "Run" is fast.
builder.Services.AddHostedService<CompilerWarmupService>();

// JSON: send enums as text ("Passed") instead of numbers (0). Minimal APIs and controllers
// each have their own JSON settings, so we configure both.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers()      // for the classes in Controllers/
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidation();      // .NET 10: Data Annotations validation for Minimal APIs too
builder.Services.AddProblemDetails();  // standard JSON error format (RFC 9457)
builder.Services.AddOpenApi();         // describes the API at /openapi/v1.json

var app = builder.Build();

// Apply pending EF Core migrations at startup (creates learn.db on first run).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// ---------- 2) Middleware pipeline (order matters!) ----------

app.UseExceptionHandler();             // catch unhandled exceptions → ProblemDetails JSON
app.UseStatusCodePages();              // empty 404/405... responses get a ProblemDetails body

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();                 // "/" → "/index.html"
app.UseStaticFiles();                  // serves the built React app from wwwroot/

// ---------- 3) Endpoints ----------

app.MapControllers();                  // Controllers/*.cs (attribute routing)
app.MapLessonEndpoints();              // Endpoints/LessonEndpoints.cs (Minimal APIs)
app.MapExerciseEndpoints();            // Endpoints/ExerciseEndpoints.cs

// Any non-API URL (e.g. /lessons/p0-types) is a React route: return index.html.
app.MapFallbackToFile("index.html");

app.Run();
