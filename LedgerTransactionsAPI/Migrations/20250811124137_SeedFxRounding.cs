using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerTransactionsAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedFxRounding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 12, 41, 37, 676, DateTimeKind.Utc).AddTicks(7905));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 12, 41, 37, 676, DateTimeKind.Utc).AddTicks(7910));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 12, 41, 37, 676, DateTimeKind.Utc).AddTicks(7912));

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Id", "AvailableBalance", "CreatedAt", "Currency", "Holder", "Version" },
                values: new object[] { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), 0m, new DateTime(2025, 8, 11, 12, 41, 37, 676, DateTimeKind.Utc).AddTicks(7914), "DOP", "FX_ROUNDING", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 12, 16, 48, 59, DateTimeKind.Utc).AddTicks(3559));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 12, 16, 48, 59, DateTimeKind.Utc).AddTicks(3561));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 11, 12, 16, 48, 59, DateTimeKind.Utc).AddTicks(3563));
        }
    }
}
