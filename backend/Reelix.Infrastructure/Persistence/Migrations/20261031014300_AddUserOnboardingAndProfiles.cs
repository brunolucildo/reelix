using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboardingAndProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_step = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_user_preferences_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    biography = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    profile_is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    favorites_are_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    watch_history_is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_user_profiles_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_favorite_titles",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite_titles", x => new { x.account_id, x.title_id });
                    table.ForeignKey(
                        name: "FK_user_favorite_titles_catalog_external_references_title_id",
                        column: x => x.title_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_favorite_titles_user_preferences_account_id",
                        column: x => x.account_id,
                        principalTable: "user_preferences",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_genre_preferences",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    genre_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_genre_preferences", x => new { x.account_id, x.content_type, x.genre_id });
                    table.ForeignKey(
                        name: "FK_user_genre_preferences_user_preferences_account_id",
                        column: x => x.account_id,
                        principalTable: "user_preferences",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_titles_title_id",
                table: "user_favorite_titles",
                column: "title_id");

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    table_name text;
                    role_name text;
                BEGIN
                    FOREACH table_name IN ARRAY ARRAY[
                        'user_profiles',
                        'user_preferences',
                        'user_genre_preferences',
                        'user_favorite_titles'
                    ] LOOP
                        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
                        EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
                        FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                            IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                                EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                            END IF;
                        END LOOP;
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_favorite_titles");

            migrationBuilder.DropTable(
                name: "user_genre_preferences");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "user_preferences");

        }
    }
}
