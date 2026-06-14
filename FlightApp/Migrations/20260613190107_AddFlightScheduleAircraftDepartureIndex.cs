using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightScheduleAircraftDepartureIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_flight_schedules_aircraft_id",
                table: "flight_schedules");

            migrationBuilder.CreateIndex(
                name: "ix_flight_schedules_aircraft_id_departure_time",
                table: "flight_schedules",
                columns: new[] { "aircraft_id", "departure_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_flight_schedules_aircraft_id_departure_time",
                table: "flight_schedules");

            migrationBuilder.CreateIndex(
                name: "ix_flight_schedules_aircraft_id",
                table: "flight_schedules",
                column: "aircraft_id");
        }
    }
}
