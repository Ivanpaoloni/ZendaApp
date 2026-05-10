using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GoogleCalendarAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleCalendarId",
                table: "Prestadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleRefreshToken",
                table: "Prestadores",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 471, DateTimeKind.Utc).AddTicks(457));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 471, DateTimeKind.Utc).AddTicks(1743));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 471, DateTimeKind.Utc).AddTicks(1749));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 470, DateTimeKind.Utc).AddTicks(492));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 470, DateTimeKind.Utc).AddTicks(1925));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 470, DateTimeKind.Utc).AddTicks(1935));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 9, 21, 57, 52, 470, DateTimeKind.Utc).AddTicks(1937));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleCalendarId",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "GoogleRefreshToken",
                table: "Prestadores");

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 925, DateTimeKind.Utc).AddTicks(2951));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 925, DateTimeKind.Utc).AddTicks(4394));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 925, DateTimeKind.Utc).AddTicks(4400));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 924, DateTimeKind.Utc).AddTicks(2994));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 924, DateTimeKind.Utc).AddTicks(4446));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 924, DateTimeKind.Utc).AddTicks(4455));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 5, 8, 15, 33, 3, 924, DateTimeKind.Utc).AddTicks(4458));
        }
    }
}
