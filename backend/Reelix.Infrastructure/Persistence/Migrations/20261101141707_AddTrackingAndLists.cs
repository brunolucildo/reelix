using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingAndLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tracking_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    watched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_records", x => x.id);
                    table.CheckConstraint("CK_tracking_records_target_type", "(item_type = 'movie' AND series_id IS NULL) OR (item_type = 'episode' AND series_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_tracking_records_catalog_external_references_series_id",
                        column: x => x.series_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tracking_records_catalog_external_references_target_id",
                        column: x => x.target_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tracking_records_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_custom_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_custom_lists", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_custom_lists_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_watchlist_titles",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_watchlist_titles", x => new { x.account_id, x.title_id });
                    table.ForeignKey(
                        name: "FK_user_watchlist_titles_catalog_external_references_title_id",
                        column: x => x.title_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_watchlist_titles_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_custom_list_titles",
                columns: table => new
                {
                    list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_custom_list_titles", x => new { x.list_id, x.title_id });
                    table.ForeignKey(
                        name: "FK_user_custom_list_titles_catalog_external_references_title_id",
                        column: x => x.title_id,
                        principalTable: "catalog_external_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_custom_list_titles_user_custom_lists_list_id",
                        column: x => x.list_id,
                        principalTable: "user_custom_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_records_account_series_target",
                table: "tracking_records",
                columns: new[] { "account_id", "series_id", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_records_account_type_watched_at",
                table: "tracking_records",
                columns: new[] { "account_id", "item_type", "watched_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_records_account_watched_at",
                table: "tracking_records",
                columns: new[] { "account_id", "watched_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_records_series_id",
                table: "tracking_records",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_records_target_id",
                table: "tracking_records",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "UX_tracking_records_account_target",
                table: "tracking_records",
                columns: new[] { "account_id", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_custom_list_titles_list_position",
                table: "user_custom_list_titles",
                columns: new[] { "list_id", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_user_custom_list_titles_title_id",
                table: "user_custom_list_titles",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_custom_lists_account_updated_at",
                table: "user_custom_lists",
                columns: new[] { "account_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_watchlist_titles_account_added_at",
                table: "user_watchlist_titles",
                columns: new[] { "account_id", "added_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_watchlist_titles_title_id",
                table: "user_watchlist_titles",
                column: "title_id");

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    table_name text;
                    role_name text;
                BEGIN
                    FOREACH table_name IN ARRAY ARRAY[
                        'tracking_records',
                        'user_custom_lists',
                        'user_custom_list_titles',
                        'user_watchlist_titles'
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
                name: "tracking_records");

            migrationBuilder.DropTable(
                name: "user_custom_list_titles");

            migrationBuilder.DropTable(
                name: "user_watchlist_titles");

            migrationBuilder.DropTable(
                name: "user_custom_lists");
        }
    }
}
