using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerTransactionsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFxValuation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FxPair",
                table: "transactions",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRate",
                table: "transactions",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCredit",
                table: "ledger_entries",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrency",
                table: "ledger_entries",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseDebit",
                table: "ledger_entries",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FxRate",
                table: "ledger_entries",
                type: "numeric(18,6)",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FxPair",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "FxRate",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "BaseCredit",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "BaseCurrency",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "BaseDebit",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "FxRate",
                table: "ledger_entries");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 9, 19, 36, 48, 951, DateTimeKind.Utc).AddTicks(2217));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 9, 19, 36, 48, 951, DateTimeKind.Utc).AddTicks(2220));

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 9, 19, 36, 48, 951, DateTimeKind.Utc).AddTicks(2223));
        }
    }
}
