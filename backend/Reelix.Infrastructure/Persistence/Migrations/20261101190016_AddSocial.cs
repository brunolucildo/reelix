using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "favorites_shared_with_friends",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_discoverable",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ratings_shared_with_friends",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ratings_shared_with_friends_since",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "watch_history_shared_with_friends",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "watch_history_shared_with_friends_since",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "friend_relationships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    second_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    addressee_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_relationships", x => x.id);
                    table.CheckConstraint("CK_friend_relationships_canonical_order", "first_account_id < second_account_id");
                    table.CheckConstraint("CK_friend_relationships_distinct_accounts", "first_account_id <> second_account_id");
                    table.CheckConstraint("CK_friend_relationships_participants", "(requester_account_id = first_account_id AND addressee_account_id = second_account_id) OR (requester_account_id = second_account_id AND addressee_account_id = first_account_id)");
                    table.CheckConstraint("CK_friend_relationships_status", "status IN ('Pending', 'Accepted', 'Declined', 'Cancelled', 'Removed')");
                    table.ForeignKey(
                        name: "FK_friend_relationships_user_accounts_first_account_id",
                        column: x => x.first_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friend_relationships_user_accounts_second_account_id",
                        column: x => x.second_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    event_version = table.Column<int>(type: "integer", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_events", x => x.id);
                    table.CheckConstraint("CK_social_events_distinct_accounts", "actor_account_id <> recipient_account_id");
                    table.CheckConstraint("CK_social_events_version_and_type", "event_version > 0 AND event_type IN ('friend.requested', 'friend.accepted')");
                    table.ForeignKey(
                        name: "FK_social_events_user_accounts_actor_account_id",
                        column: x => x.actor_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_social_events_user_accounts_recipient_account_id",
                        column: x => x.recipient_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_display_name",
                table: "user_profiles",
                column: "display_name");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_profiles_friend_history_consent",
                table: "user_profiles",
                sql: "watch_history_shared_with_friends = (watch_history_shared_with_friends_since IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_profiles_friend_ratings_consent",
                table: "user_profiles",
                sql: "ratings_shared_with_friends = (ratings_shared_with_friends_since IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_friend_relationships_incoming",
                table: "friend_relationships",
                columns: new[] { "addressee_account_id", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_friend_relationships_outgoing",
                table: "friend_relationships",
                columns: new[] { "requester_account_id", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_friend_relationships_second_account_id",
                table: "friend_relationships",
                column: "second_account_id");

            migrationBuilder.CreateIndex(
                name: "UX_friend_relationships_pair",
                table: "friend_relationships",
                columns: new[] { "first_account_id", "second_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_events_actor_account_id",
                table: "social_events",
                column: "actor_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_social_events_recipient_occurred_at",
                table: "social_events",
                columns: new[] { "recipient_account_id", "occurred_at" });

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    table_name text;
                    role_name text;
                BEGIN
                    FOREACH table_name IN ARRAY ARRAY[
                        'friend_relationships',
                        'social_events'
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
                name: "friend_relationships");

            migrationBuilder.DropTable(
                name: "social_events");

            migrationBuilder.DropIndex(
                name: "IX_user_profiles_display_name",
                table: "user_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_user_profiles_friend_history_consent",
                table: "user_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_user_profiles_friend_ratings_consent",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "favorites_shared_with_friends",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "is_discoverable",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "ratings_shared_with_friends",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "ratings_shared_with_friends_since",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "watch_history_shared_with_friends",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "watch_history_shared_with_friends_since",
                table: "user_profiles");
        }
    }
}
