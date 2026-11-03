using LearnBackend.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnBackend.Api.Data;

/// <summary>
/// The DbContext is EF Core's "session" with the database: it tracks entity changes
/// and turns LINQ queries into SQL. It is registered as Scoped (one instance per HTTP request)
/// by builder.Services.AddDbContext in Program.cs.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<ExerciseProgress> ExerciseProgress => Set<ExerciseProgress>();
    public DbSet<TermReview> TermReviews => Set<TermReview>();
    public DbSet<ActivityDay> ActivityDays => Set<ActivityDay>();

    // Fluent API: configuration that can't be expressed by conventions alone.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LessonProgress>(e =>
        {
            e.HasIndex(x => x.LessonId).IsUnique();
            e.Property(x => x.LessonId).HasMaxLength(100);
        });

        modelBuilder.Entity<ExerciseProgress>(e =>
        {
            e.HasIndex(x => x.ExerciseId).IsUnique();
            e.Property(x => x.ExerciseId).HasMaxLength(100);
        });

        modelBuilder.Entity<TermReview>(e =>
        {
            e.HasIndex(x => x.TermId).IsUnique();
            e.HasIndex(x => x.DueAt);
            e.Property(x => x.TermId).HasMaxLength(100);
        });

        modelBuilder.Entity<ActivityDay>().HasKey(x => x.Date);
    }
}
