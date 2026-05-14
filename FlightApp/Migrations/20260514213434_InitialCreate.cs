using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:booking_status", "upcoming,completed,cancelled")
                .Annotation("Npgsql:Enum:cabin_class", "economy,premium_economy,business,first")
                .Annotation("Npgsql:Enum:notif_type", "booking,alert,reminder,promo")
                .Annotation("Npgsql:Enum:passenger_type", "adult,child,infant")
                .Annotation("Npgsql:Enum:seat_side", "window,middle,aisle")
                .Annotation("Npgsql:Enum:trip_type", "one_way,round_trip");

            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    code = table.Column<string>(type: "char(3)", nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    timezone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_airports", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    origin = table.Column<string>(type: "char(3)", nullable: false),
                    destination = table.Column<string>(type: "char(3)", nullable: false),
                    airline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    flight_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "USD"),
                    departure_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    stops = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    baggage_included = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    refundable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flights", x => x.id);
                    table.ForeignKey(
                        name: "fk_flights_airports_destination",
                        column: x => x.destination,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_flights_airports_origin",
                        column: x => x.origin,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "notif_type", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "char(3)", nullable: false),
                    destination = table.Column<string>(type: "char(3)", nullable: false),
                    cabin = table.Column<int>(type: "cabin_class", nullable: false),
                    target_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    date_from = table.Column<DateOnly>(type: "date", nullable: true),
                    date_to = table.Column<DateOnly>(type: "date", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_alerts_airports_destination",
                        column: x => x.destination,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_alerts_airports_origin",
                        column: x => x.origin,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_alerts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_travelers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    passport_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    passport_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_travelers", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_travelers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "search_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "char(3)", nullable: false),
                    destination = table.Column<string>(type: "char(3)", nullable: false),
                    depart_date = table.Column<DateOnly>(type: "date", nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    trip_type = table.Column<int>(type: "trip_type", nullable: false),
                    cabin = table.Column<int>(type: "cabin_class", nullable: false),
                    adults = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    searched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_search_history_airports_destination",
                        column: x => x.destination,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_search_history_airports_origin",
                        column: x => x.origin,
                        principalTable: "airports",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_search_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_loyalty",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequent_flyer_program = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    frequent_flyer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_loyalty", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_loyalty_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_passports",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passport_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_passports", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_passports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preferred_cabin = table.Column<int>(type: "cabin_class", nullable: true),
                    preferred_meal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    preferred_seat = table.Column<int>(type: "seat_side", nullable: true),
                    preferred_currency = table.Column<string>(type: "char(3)", nullable: true),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sms_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cabin = table.Column<int>(type: "cabin_class", nullable: false),
                    trip_type = table.Column<int>(type: "trip_type", nullable: false),
                    status = table.Column<int>(type: "booking_status", nullable: false, defaultValueSql: "'upcoming'::booking_status"),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    passenger_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    base_fare = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    taxes_fees = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    booked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "flight_prices",
                columns: table => new
                {
                    flight_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cabin = table.Column<int>(type: "cabin_class", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    total_seats = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flight_prices", x => new { x.flight_id, x.cabin });
                    table.ForeignKey(
                        name: "fk_flight_prices_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_flights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cabin = table.Column<int>(type: "cabin_class", nullable: false),
                    adults = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    depart_date = table.Column<DateOnly>(type: "date", nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    price_at_save = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_flights", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_flights_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_saved_flights_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_passengers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    passport_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    passport_expiry = table.Column<DateOnly>(type: "date", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    seat_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    special_requests = table.Column<string>(type: "text", nullable: true),
                    frequent_flyer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fare = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    passenger_type = table.Column<int>(type: "passenger_type", nullable: false, defaultValueSql: "'adult'::passenger_type")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_passengers", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_passengers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_passengers_booking_id_seat_number",
                table: "booking_passengers",
                columns: new[] { "booking_id", "seat_number" },
                unique: true,
                filter: "seat_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_flight_id",
                table: "bookings",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_reference",
                table: "bookings",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_user_id",
                table: "bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_flights_destination",
                table: "flights",
                column: "destination");

            migrationBuilder.CreateIndex(
                name: "ix_flights_origin",
                table: "flights",
                column: "origin");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_alerts_destination",
                table: "price_alerts",
                column: "destination");

            migrationBuilder.CreateIndex(
                name: "ix_price_alerts_origin",
                table: "price_alerts",
                column: "origin");

            migrationBuilder.CreateIndex(
                name: "ix_price_alerts_user_id",
                table: "price_alerts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_flights_flight_id",
                table: "saved_flights",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_flights_user_id_flight_id_cabin_depart_date",
                table: "saved_flights",
                columns: new[] { "user_id", "flight_id", "cabin", "depart_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saved_travelers_user_id",
                table: "saved_travelers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_history_destination",
                table: "search_history",
                column: "destination");

            migrationBuilder.CreateIndex(
                name: "ix_search_history_origin",
                table: "search_history",
                column: "origin");

            migrationBuilder.CreateIndex(
                name: "ix_search_history_user_id",
                table: "search_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_passengers");

            migrationBuilder.DropTable(
                name: "flight_prices");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "price_alerts");

            migrationBuilder.DropTable(
                name: "saved_flights");

            migrationBuilder.DropTable(
                name: "saved_travelers");

            migrationBuilder.DropTable(
                name: "search_history");

            migrationBuilder.DropTable(
                name: "user_loyalty");

            migrationBuilder.DropTable(
                name: "user_passports");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "flights");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "airports");
        }
    }
}
