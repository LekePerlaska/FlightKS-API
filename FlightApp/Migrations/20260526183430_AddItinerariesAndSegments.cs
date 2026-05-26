using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class AddItinerariesAndSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "itinerary_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "itineraries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    origin_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    departure_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    stops_count = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itineraries", x => x.id);
                    table.ForeignKey(
                        name: "fk_itineraries_airports_destination_airport_id",
                        column: x => x.destination_airport_id,
                        principalTable: "airports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_itineraries_airports_origin_airport_id",
                        column: x => x.origin_airport_id,
                        principalTable: "airports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itinerary_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    itinerary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_order = table.Column<int>(type: "integer", nullable: false),
                    layover_minutes_after_segment = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itinerary_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_itinerary_segments_flight_schedules_flight_schedule_id",
                        column: x => x.flight_schedule_id,
                        principalTable: "flight_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_itinerary_segments_itineraries_itinerary_id",
                        column: x => x.itinerary_id,
                        principalTable: "itineraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_itinerary_id",
                table: "bookings",
                column: "itinerary_id");

            migrationBuilder.CreateIndex(
                name: "ix_itineraries_destination_airport_id",
                table: "itineraries",
                column: "destination_airport_id");

            migrationBuilder.CreateIndex(
                name: "ix_itineraries_origin_airport_id_destination_airport_id_depart",
                table: "itineraries",
                columns: new[] { "origin_airport_id", "destination_airport_id", "departure_time" });

            migrationBuilder.CreateIndex(
                name: "ix_itinerary_segments_flight_schedule_id",
                table: "itinerary_segments",
                column: "flight_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_itinerary_segments_itinerary_id_segment_order",
                table: "itinerary_segments",
                columns: new[] { "itinerary_id", "segment_order" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_itineraries_itinerary_id",
                table: "bookings",
                column: "itinerary_id",
                principalTable: "itineraries",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_itineraries_itinerary_id",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "itinerary_segments");

            migrationBuilder.DropTable(
                name: "itineraries");

            migrationBuilder.DropIndex(
                name: "ix_bookings_itinerary_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "itinerary_id",
                table: "bookings");
        }
    }
}
