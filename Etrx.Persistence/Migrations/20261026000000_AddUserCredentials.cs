using Etrx.Persistence.Databases;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Etrx.Persistence.Migrations;

[DbContext(typeof(EtrxDbContext))]
[Migration("20261026000000_AddUserCredentials")]
public class AddUserCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_credentials",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                security_stamp = table.Column<string>(type: "text", nullable: false),
                admin_activated = table.Column<bool>(type: "boolean", nullable: false),
                failed_attempts = table.Column<int>(type: "integer", nullable: false),
                locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_credentials", c => c.user_id);
                table.ForeignKey("fk_user_credentials_users_user_id", c => c.user_id,
                    "users", "id", onDelete: ReferentialAction.Cascade);
            });
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("user_credentials");
}
