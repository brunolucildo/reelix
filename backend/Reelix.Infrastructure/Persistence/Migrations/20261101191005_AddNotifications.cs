using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expo_push_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    platform = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_devices", x => x.id);
                    table.CheckConstraint("CK_notification_devices_platform", "platform IN ('Ios', 'Android')");
                    table.ForeignKey(
                        name: "FK_notification_devices_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    social_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    new_episode_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    premiere_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_processed_social_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_processed_social_events", x => x.event_id);
                    table.CheckConstraint("CK_notification_processed_social_events_outcome", "outcome IN ('created', 'disabled', 'obsolete')");
                    table.ForeignKey(
                        name: "FK_notification_processed_social_events_social_events_event_id",
                        column: x => x.event_id,
                        principalTable: "social_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    body = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    deep_link = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => x.id);
                    table.CheckConstraint("CK_user_notifications_category", "category IN ('Social', 'NewEpisode', 'Premiere')");
                    table.ForeignKey(
                        name: "FK_user_notifications_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expo_ticket_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expo_ticket_created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    receipt_check_count = table.Column<int>(type: "integer", nullable: false),
                    receipt_check_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                    table.CheckConstraint("CK_notification_deliveries_status", "status IN ('Pending', 'Ticketed', 'Completed', 'Failed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_notification_deliveries_notification_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "notification_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_user_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "user_notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_device_id",
                table: "notification_deliveries",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_pending",
                table: "notification_deliveries",
                columns: new[] { "status", "next_attempt_at", "lease_until" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_receipts",
                table: "notification_deliveries",
                columns: new[] { "status", "receipt_check_at" });

            migrationBuilder.CreateIndex(
                name: "UX_notification_deliveries_notification_device",
                table: "notification_deliveries",
                columns: new[] { "notification_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_devices_account_active",
                table: "notification_devices",
                columns: new[] { "account_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "UX_notification_devices_active_token",
                table: "notification_devices",
                column: "expo_push_token",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "UX_notification_devices_installation",
                table: "notification_devices",
                column: "installation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_account_created",
                table: "user_notifications",
                columns: new[] { "account_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "UX_user_notifications_account_source",
                table: "user_notifications",
                columns: new[] { "account_id", "source_key" },
                unique: true);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    table_name text;
                    role_name text;
                BEGIN
                    FOREACH table_name IN ARRAY ARRAY[
                        'notification_preferences',
                        'notification_devices',
                        'user_notifications',
                        'notification_deliveries',
                        'notification_processed_social_events'
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
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_processed_social_events");

            migrationBuilder.DropTable(
                name: "notification_devices");

            migrationBuilder.DropTable(
                name: "user_notifications");
        }
    }
}
