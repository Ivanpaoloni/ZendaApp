using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArquitecturaLimpia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Negocios_PlanSuscripcion_PlanSuscripcionId",
                table: "Negocios");

            migrationBuilder.DropIndex(
                name: "IX_Negocios_PlanSuscripcionId",
                table: "Negocios");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_NegocioId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "PlanSuscripcionId",
                table: "Negocios");

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

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NegocioId_Email",
                table: "Clientes",
                columns: new[] { "NegocioId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_NegocioId_Email",
                table: "Clientes");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanSuscripcionId",
                table: "Negocios",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.CreateIndex(
                name: "IX_Negocios_PlanSuscripcionId",
                table: "Negocios",
                column: "PlanSuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NegocioId",
                table: "Clientes",
                column: "NegocioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Negocios_PlanSuscripcion_PlanSuscripcionId",
                table: "Negocios",
                column: "PlanSuscripcionId",
                principalTable: "PlanSuscripcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
