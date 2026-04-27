using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperadminPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioMensualPersonalizado",
                table: "SuscripcionesNegocio",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Negocios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotasAdmin",
                table: "Negocios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComprobanteUrl",
                table: "HistorialPagos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "HistorialPagos",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 976, DateTimeKind.Utc).AddTicks(9673));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 977, DateTimeKind.Utc).AddTicks(955));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 977, DateTimeKind.Utc).AddTicks(962));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 975, DateTimeKind.Utc).AddTicks(8425));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 975, DateTimeKind.Utc).AddTicks(9992));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 976, DateTimeKind.Utc).AddTicks(92));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 26, 22, 15, 7, 976, DateTimeKind.Utc).AddTicks(109));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioMensualPersonalizado",
                table: "SuscripcionesNegocio");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "NotasAdmin",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "ComprobanteUrl",
                table: "HistorialPagos");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "HistorialPagos");

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 630, DateTimeKind.Utc).AddTicks(9374));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 631, DateTimeKind.Utc).AddTicks(610));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 631, DateTimeKind.Utc).AddTicks(616));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 629, DateTimeKind.Utc).AddTicks(9428));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 630, DateTimeKind.Utc).AddTicks(788));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 630, DateTimeKind.Utc).AddTicks(797));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 14, 58, 58, 630, DateTimeKind.Utc).AddTicks(799));
        }
    }
}
