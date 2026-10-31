using Etrx.Persistence.Databases;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Etrx.Persistence.Migrations;

[DbContext(typeof(EtrxDbContext))]
[Migration("20261028000000_AddUserProblemProgress")]
public class AddUserProblemProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("user_problem_progress", table => new
        {
            user_id = table.Column<Guid>("uuid", nullable: false),
            contest_id = table.Column<int>("integer", nullable: false),
            index = table.Column<string>("text", nullable: false),
            is_solved = table.Column<bool>("boolean", nullable: false),
            has_failed_attempt = table.Column<bool>("boolean", nullable: false),
            is_deferred = table.Column<bool>("boolean", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_user_problem_progress", p => new { p.user_id, p.contest_id, p.index });
            table.ForeignKey("fk_user_problem_progress_users_user_id", p => p.user_id, "users", "id",
                onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("user_problem_sync", table => new
        {
            user_id = table.Column<Guid>("uuid", nullable: false),
            last_attempt_at = table.Column<DateTimeOffset>("timestamp with time zone", nullable: true),
            last_success_at = table.Column<DateTimeOffset>("timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_user_problem_sync", p => p.user_id);
            table.ForeignKey("fk_user_problem_sync_users_user_id", p => p.user_id, "users", "id",
                onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.DropIndex("ix_submissions_user_id", "submissions");
        migrationBuilder.CreateIndex("ix_submissions_user_id_contest_id_index_verdict", "submissions",
            new[] { "user_id", "contest_id", "index", "verdict" });

        // Restore ALL historical protocol rows, not only the current table page or session.
        migrationBuilder.Sql("""
            INSERT INTO user_problem_progress (user_id, contest_id, index, is_solved, has_failed_attempt, is_deferred)
            SELECT user_id, contest_id, index, bool_or(verdict = 'OK'),
                bool_or(verdict IN ('FAILED', 'PARTIAL', 'COMPILATION_ERROR', 'RUNTIME_ERROR', 'WRONG_ANSWER',
                    'TIME_LIMIT_EXCEEDED', 'MEMORY_LIMIT_EXCEEDED', 'IDLENESS_LIMIT_EXCEEDED',
                    'SECURITY_VIOLATED', 'CRASHED', 'CHALLENGED', 'REJECTED')), FALSE
            FROM submissions
            WHERE verdict IS NOT NULL
            GROUP BY user_id, contest_id, index;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("user_problem_sync");
        migrationBuilder.DropTable("user_problem_progress");
        migrationBuilder.DropIndex("ix_submissions_user_id_contest_id_index_verdict", "submissions");
        migrationBuilder.CreateIndex("ix_submissions_user_id", "submissions", "user_id");
    }
}
