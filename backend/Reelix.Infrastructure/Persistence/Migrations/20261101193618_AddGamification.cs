using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "metadata_updated_at",
                table: "catalog_external_references",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "metadata_version",
                table: "catalog_external_references",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "runtime_minutes",
                table: "catalog_external_references",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "achievement_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    rule_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    threshold = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_definitions", x => x.id);
                    table.CheckConstraint("CK_achievement_definitions_rule_type", "rule_type IN ('MovieCount', 'EpisodeCount', 'SeriesCount', 'MovieGenreCount')");
                    table.CheckConstraint("CK_achievement_definitions_threshold_version", "threshold > 0 AND version > 0");
                });

            migrationBuilder.CreateTable(
                name: "catalog_reference_genres",
                columns: table => new
                {
                    catalog_reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_reference_genres", x => new { x.catalog_reference_id, x.genre_id });
                    table.CheckConstraint("CK_catalog_reference_genres_positive_id", "genre_id > 0");
                    table.ForeignKey(
                        name: "FK_catalog_reference_genres_catalog_external_references_catalo~",
                        column: x => x.catalog_reference_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gamification_preferences",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badges_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ranking_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gamification_preferences", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_gamification_preferences_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    first_earned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_earned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => new { x.account_id, x.definition_id });
                    table.CheckConstraint("CK_user_achievements_status", "status IN ('Earned', 'Revoked')");
                    table.ForeignKey(
                        name: "FK_user_achievements_achievement_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "achievement_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_achievements_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "achievement_definitions",
                columns: new[] { "id", "code", "description", "is_active", "rule_type", "threshold", "title", "version" },
                values: new object[,]
                {
                    { new Guid("09000000-0000-0000-0000-000000000001"), "first-movie", "Registre seu primeiro filme assistido.", true, "MovieCount", 1, "Primeiro filme", 1 },
                    { new Guid("09000000-0000-0000-0000-000000000002"), "cinephile", "Registre 10 filmes distintos como assistidos.", true, "MovieCount", 10, "Cinéfilo", 1 },
                    { new Guid("09000000-0000-0000-0000-000000000003"), "first-marathon", "Registre 10 episódios distintos como assistidos.", true, "EpisodeCount", 10, "Maratona inicial", 1 },
                    { new Guid("09000000-0000-0000-0000-000000000004"), "series-in-focus", "Assista a pelo menos um episódio em três séries distintas.", true, "SeriesCount", 3, "Séries em foco", 1 },
                    { new Guid("09000000-0000-0000-0000-000000000005"), "genre-explorer", "Assista a filmes de cinco gêneros canônicos distintos.", true, "MovieGenreCount", 5, "Explorador de gêneros", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "UX_achievement_definitions_code",
                table: "achievement_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_reference_genres_genre_id",
                table: "catalog_reference_genres",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_definition_id",
                table: "user_achievements",
                column: "definition_id");

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    table_name text;
                    role_name text;
                BEGIN
                    FOREACH table_name IN ARRAY ARRAY[
                        'user_achievements',
                        'gamification_preferences'
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
                name: "catalog_reference_genres");

            migrationBuilder.DropTable(
                name: "gamification_preferences");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "achievement_definitions");

            migrationBuilder.DropColumn(
                name: "metadata_updated_at",
                table: "catalog_external_references");

            migrationBuilder.DropColumn(
                name: "metadata_version",
                table: "catalog_external_references");

            migrationBuilder.DropColumn(
                name: "runtime_minutes",
                table: "catalog_external_references");
        }
    }
}
