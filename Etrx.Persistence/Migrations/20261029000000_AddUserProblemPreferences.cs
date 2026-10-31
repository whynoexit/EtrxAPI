using Etrx.Persistence.Databases;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Etrx.Persistence.Migrations;

[DbContext(typeof(EtrxDbContext))]
[Migration("20261029000000_AddUserProblemPreferences")]
public class AddUserProblemPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("user_problem_preferences", table => new
        {
            user_id = table.Column<Guid>("uuid", nullable: false),
            filters_json = table.Column<string>("text", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_user_problem_preferences", p => p.user_id);
            table.ForeignKey("fk_user_problem_preferences_users_user_id", p => p.user_id, "users", "id",
                onDelete: ReferentialAction.Cascade);
        });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("user_problem_preferences");
}
