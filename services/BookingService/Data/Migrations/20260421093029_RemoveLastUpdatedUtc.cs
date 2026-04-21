using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastUpdatedUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedUtc",
                table: "BookInventories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedUtc",
                table: "BookInventories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "BookInventories",
                keyColumn: "BookId",
                keyValue: "b1",
                column: "LastUpdatedUtc",
                value: new DateTime(2026, 4, 21, 8, 35, 30, 247, DateTimeKind.Utc).AddTicks(8350));

            migrationBuilder.UpdateData(
                table: "BookInventories",
                keyColumn: "BookId",
                keyValue: "b2",
                column: "LastUpdatedUtc",
                value: new DateTime(2026, 4, 21, 8, 35, 30, 247, DateTimeKind.Utc).AddTicks(8550));

            migrationBuilder.UpdateData(
                table: "BookInventories",
                keyColumn: "BookId",
                keyValue: "b3",
                column: "LastUpdatedUtc",
                value: new DateTime(2026, 4, 21, 8, 35, 30, 247, DateTimeKind.Utc).AddTicks(8550));
        }
    }
}
