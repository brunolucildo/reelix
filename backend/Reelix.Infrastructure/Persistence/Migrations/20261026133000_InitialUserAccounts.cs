using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Reelix.Infrastructure.Persistence;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ReelixDbContext))]
[Migration("20261026133000_InitialUserAccounts")]
public sealed class InitialUserAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_accounts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                supabase_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_accounts", account => account.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_user_accounts_supabase_user_id",
            table: "user_accounts",
            column: "supabase_user_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_accounts");
    }
}
