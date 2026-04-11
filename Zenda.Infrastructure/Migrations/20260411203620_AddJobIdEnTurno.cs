using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobIdEnTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecordatorioJobId",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 11, 20, 36, 18, 554, DateTimeKind.Utc).AddTicks(2618));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 11, 20, 36, 18, 554, DateTimeKind.Utc).AddTicks(4075));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 11, 20, 36, 18, 554, DateTimeKind.Utc).AddTicks(4082));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 11, 20, 36, 18, 554, DateTimeKind.Utc).AddTicks(4085));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordatorioJobId",
                table: "Turnos");

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 10, 19, 22, 4, 205, DateTimeKind.Utc).AddTicks(275));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 10, 19, 22, 4, 205, DateTimeKind.Utc).AddTicks(1692));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 10, 19, 22, 4, 205, DateTimeKind.Utc).AddTicks(1700));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 10, 19, 22, 4, 205, DateTimeKind.Utc).AddTicks(1703));
        }
    }
}
