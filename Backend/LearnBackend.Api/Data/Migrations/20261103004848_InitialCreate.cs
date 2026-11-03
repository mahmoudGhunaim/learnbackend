using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityDays",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Actions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDays", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExerciseId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SavedCode = table.Column<string>(type: "TEXT", nullable: true),
                    HintsUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    BestPassed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTests = table.Column<int>(type: "INTEGER", nullable: false),
                    SolutionViewed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LessonProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LessonId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    QuizBestScore = table.Column<int>(type: "INTEGER", nullable: false),
                    QuizTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    QuizAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastVisitedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TermReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TermId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Repetitions = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervalDays = table.Column<double>(type: "REAL", nullable: false),
                    EaseFactor = table.Column<double>(type: "REAL", nullable: false),
                    DueAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WrongCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseProgress_ExerciseId",
                table: "ExerciseProgress",
                column: "ExerciseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_LessonId",
                table: "LessonProgress",
                column: "LessonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TermReviews_DueAt",
                table: "TermReviews",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_TermReviews_TermId",
                table: "TermReviews",
                column: "TermId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDays");

            migrationBuilder.DropTable(
                name: "ExerciseProgress");

            migrationBuilder.DropTable(
                name: "LessonProgress");

            migrationBuilder.DropTable(
                name: "TermReviews");
        }
    }
}
