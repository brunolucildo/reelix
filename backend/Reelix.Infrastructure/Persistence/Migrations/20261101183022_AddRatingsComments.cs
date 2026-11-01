using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingsComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_episode_series",
                columns: table => new
                {
                    episode_id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_episode_series", x => x.episode_id);
                    table.CheckConstraint("CK_catalog_episode_series_distinct_ids", "episode_id <> series_id");
                    table.ForeignKey(
                        name: "FK_catalog_episode_series_catalog_external_references_episode_~",
                        column: x => x.episode_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_episode_series_catalog_external_references_series_id",
                        column: x => x.series_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    contains_spoiler = table.Column<bool>(type: "boolean", nullable: false),
                    possible_spoiler = table.Column<bool>(type: "boolean", nullable: false),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    automated_analysis_requested = table.Column<bool>(type: "boolean", nullable: false),
                    moderation_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    moderation_version = table.Column<int>(type: "integer", nullable: false),
                    moderation_attempts = table.Column<int>(type: "integer", nullable: false),
                    moderation_lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_comments", x => x.id);
                    table.CheckConstraint("CK_user_comments_audience", "audience IN ('private', 'public')");
                    table.CheckConstraint("CK_user_comments_content_length", "length(btrim(content)) BETWEEN 1 AND 2000");
                    table.CheckConstraint("CK_user_comments_moderation_status", "moderation_status IN ('pending', 'published', 'blocked')");
                    table.CheckConstraint("CK_user_comments_moderation_version", "moderation_version > 0 AND moderation_attempts >= 0");
                    table.CheckConstraint("CK_user_comments_target", "(target_type = 'episode' AND series_id IS NOT NULL) OR (target_type IN ('movie', 'series') AND series_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_user_comments_catalog_external_references_series_id",
                        column: x => x.series_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_comments_catalog_external_references_target_id",
                        column: x => x.target_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_comments_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    score = table.Column<decimal>(type: "numeric(2,1)", nullable: false),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ratings", x => x.id);
                    table.CheckConstraint("CK_user_ratings_audience", "audience IN ('private', 'public')");
                    table.CheckConstraint("CK_user_ratings_score", "score >= 0 AND score <= 5 AND score * 2 = trunc(score * 2)");
                    table.CheckConstraint("CK_user_ratings_target", "(target_type = 'episode' AND series_id IS NOT NULL) OR (target_type IN ('movie', 'series') AND series_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_user_ratings_catalog_external_references_series_id",
                        column: x => x.series_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_ratings_catalog_external_references_target_id",
                        column: x => x.target_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_ratings_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_episode_series_series_id",
                table: "catalog_episode_series",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_comments_account_target_created",
                table: "user_comments",
                columns: new[] { "account_id", "target_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_comments_moderation_queue",
                table: "user_comments",
                columns: new[] { "moderation_status", "moderation_lease_until" });

            migrationBuilder.CreateIndex(
                name: "IX_user_comments_series_id",
                table: "user_comments",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_comments_target_visibility_created",
                table: "user_comments",
                columns: new[] { "target_id", "target_type", "audience", "moderation_status", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_user_ratings_series_id",
                table: "user_ratings",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_ratings_target_audience",
                table: "user_ratings",
                columns: new[] { "target_id", "target_type", "audience" });

            migrationBuilder.CreateIndex(
                name: "UX_user_ratings_account_target",
                table: "user_ratings",
                columns: new[] { "account_id", "target_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    table_name text;
                    role_name text;
                BEGIN
                    FOREACH table_name IN ARRAY ARRAY[
                        'catalog_episode_series',
                        'user_ratings',
                        'user_comments'
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
                name: "catalog_episode_series");

            migrationBuilder.DropTable(
                name: "user_comments");

            migrationBuilder.DropTable(
                name: "user_ratings");
        }
    }
}
