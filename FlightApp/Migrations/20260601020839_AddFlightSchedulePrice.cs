using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightSchedulePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cabin_class",
                table: "bookings",
                type: "seat_class",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "flight_schedule_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    flight_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_class = table.Column<int>(type: "seat_class", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flight_schedule_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_flight_schedule_prices_flight_schedules_flight_schedule_id",
                        column: x => x.flight_schedule_id,
                        principalTable: "flight_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_flight_schedule_prices_flight_schedule_id_seat_class",
                table: "flight_schedule_prices",
                columns: new[] { "flight_schedule_id", "seat_class" },
                unique: true);

            // Backfill a fare for every existing schedule, one row per distinct
            // seat class on its aircraft, derived from the base fare × multiplier.
            migrationBuilder.Sql(@"
                INSERT INTO flight_schedule_prices (id, flight_schedule_id, seat_class, price, created_at, updated_at)
                SELECT gen_random_uuid(), fs.id, sc.seat_class,
                       ROUND(fs.current_price * (CASE sc.seat_class
                            WHEN 'economy' THEN 1.0
                            WHEN 'premium_economy' THEN 1.5
                            WHEN 'business' THEN 2.5
                            WHEN 'first' THEN 4.0
                            ELSE 1.0 END), 2),
                       now(), now()
                FROM flight_schedules fs
                JOIN (
                    SELECT DISTINCT s.aircraft_id, s.seat_class
                    FROM seats s
                    WHERE s.deleted_at IS NULL
                ) sc ON sc.aircraft_id = fs.aircraft_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flight_schedule_prices");

            migrationBuilder.DropColumn(
                name: "cabin_class",
                table: "bookings");
        }
    }
}
