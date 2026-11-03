using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_key",
                table: "user_profiles",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_profiles_avatar_key",
                table: "user_profiles",
                sql: "avatar_key IS NULL OR avatar_key IN ('noir', 'cosmic', 'phantom', 'comedy', 'animation', 'adventure')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_user_profiles_avatar_key",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "avatar_key",
                table: "user_profiles");
        }
    }
}
