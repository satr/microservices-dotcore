using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookInventories",
                columns: table => new
                {
                    BookId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookInventories", x => x.BookId);
                });

            migrationBuilder.InsertData(
                table: "BookInventories",
                columns: new[] { "BookId", "LastUpdatedUtc", "Stock" },
                values: new object[,]
                {
                    { "b1", new DateTime(2026, 4, 21, 8, 35, 30, 247, DateTimeKind.Utc).AddTicks(8350), 10 },
                    { "b2", new DateTime(2026, 4, 21, 8, 35, 30, 247, DateTimeKind.Utc).AddTicks(8550), 10 },
                    { "b3", new DateTime(2026, 4, 21, 8, 35, 30, 247, DateTimeKind.Utc).AddTicks(8550), 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookInventories");
        }
    }
}
