using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFeatureFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_flags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_key",
                table: "feature_flags",
                column: "key",
                unique: true);
        }
    }
}
