using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Reelix.Infrastructure.Persistence;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ReelixDbContext))]
[Migration("20261026193000_AddExternalCatalogReferences")]
public sealed class AddExternalCatalogReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "catalog_external_references",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_catalog_external_references", reference => reference.id);
            });

        migrationBuilder.CreateIndex(
            name: "UX_catalog_external_references_external_key",
            table: "catalog_external_references",
            columns: new[] { "provider", "external_id", "content_type" },
            unique: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE public.catalog_external_references ENABLE ROW LEVEL SECURITY;
            REVOKE ALL ON TABLE public.catalog_external_references FROM PUBLIC;
            DO $$
            DECLARE
                role_name text;
            BEGIN
                FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                        EXECUTE format(
                            'REVOKE ALL ON TABLE public.catalog_external_references FROM %I',
                            role_name);
                    END IF;
                END LOOP;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "catalog_external_references");
    }
}
