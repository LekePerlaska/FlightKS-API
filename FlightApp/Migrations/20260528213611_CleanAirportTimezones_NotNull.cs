using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class CleanAirportTimezones_NotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete airports with NULL or invalid IANA timezone values.
            // Valid IANA timezones always contain a '/' (e.g. "Europe/London").
            // Test airports created during development have values like "hd", "drh", "dvs", etc.
            migrationBuilder.Sql(@"
                DELETE FROM airports
                WHERE time_zone IS NULL
                   OR time_zone NOT LIKE '%/%';
            ");

            migrationBuilder.AlterColumn<string>(
                name: "time_zone",
                table: "airports",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "time_zone",
                table: "airports",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);
        }
    }
}
