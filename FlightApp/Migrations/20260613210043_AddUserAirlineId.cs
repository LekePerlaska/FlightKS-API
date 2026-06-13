using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightKS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAirlineId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "airline_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_airline_id",
                table: "users",
                column: "airline_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_airlines_airline_id",
                table: "users",
                column: "airline_id",
                principalTable: "airlines",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_airlines_airline_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_airline_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "airline_id",
                table: "users");
        }
    }
}
