using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFixedAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_user_profiles_avatar_key",
                table: "user_profiles");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_profiles_avatar_key",
                table: "user_profiles",
                sql: "avatar_key IS NULL OR avatar_key IN ('noir', 'cosmic', 'phantom', 'comedy', 'animation', 'adventure', 'robot', 'vampire', 'maestro')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_user_profiles_avatar_key",
                table: "user_profiles");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_profiles_avatar_key",
                table: "user_profiles",
                sql: "avatar_key IS NULL OR avatar_key IN ('noir', 'cosmic', 'phantom', 'comedy', 'animation', 'adventure')");
        }
    }
}
