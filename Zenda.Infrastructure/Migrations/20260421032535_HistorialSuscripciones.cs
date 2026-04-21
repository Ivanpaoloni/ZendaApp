using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HistorialSuscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Clientes_ClienteId",
                table: "Turnos");

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPlanId",
                table: "PlanSuscripcion",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Clientes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Clientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Clientes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "SuscripcionesNegocio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanSuscripcionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MercadoPagoPreapprovalId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuscripcionesNegocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuscripcionesNegocio_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuscripcionesNegocio_PlanSuscripcion_PlanSuscripcionId",
                        column: x => x.PlanSuscripcionId,
                        principalTable: "PlanSuscripcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuscripcionNegocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    MontoCobrado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MercadoPagoPaymentId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DetalleRecibo = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPagos_SuscripcionesNegocio_SuscripcionNegocioId",
                        column: x => x.SuscripcionNegocioId,
                        principalTable: "SuscripcionesNegocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAtUtc", "MercadoPagoPlanId" },
                values: new object[] { new DateTime(2026, 4, 21, 3, 25, 33, 755, DateTimeKind.Utc).AddTicks(298), null });

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAtUtc", "MercadoPagoPlanId" },
                values: new object[] { new DateTime(2026, 4, 21, 3, 25, 33, 755, DateTimeKind.Utc).AddTicks(1706), null });

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAtUtc", "MercadoPagoPlanId" },
                values: new object[] { new DateTime(2026, 4, 21, 3, 25, 33, 755, DateTimeKind.Utc).AddTicks(1711), null });

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 21, 3, 25, 33, 754, DateTimeKind.Utc).AddTicks(2856));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 21, 3, 25, 33, 754, DateTimeKind.Utc).AddTicks(4306));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 21, 3, 25, 33, 754, DateTimeKind.Utc).AddTicks(4316));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 21, 3, 25, 33, 754, DateTimeKind.Utc).AddTicks(4318));

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NegocioId",
                table: "Clientes",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPagos_SuscripcionNegocioId",
                table: "HistorialPagos",
                column: "SuscripcionNegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_SuscripcionesNegocio_NegocioId",
                table: "SuscripcionesNegocio",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_SuscripcionesNegocio_PlanSuscripcionId",
                table: "SuscripcionesNegocio",
                column: "PlanSuscripcionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Negocios_NegocioId",
                table: "Clientes",
                column: "NegocioId",
                principalTable: "Negocios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Clientes_ClienteId",
                table: "Turnos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Negocios_NegocioId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Clientes_ClienteId",
                table: "Turnos");

            migrationBuilder.DropTable(
                name: "HistorialPagos");

            migrationBuilder.DropTable(
                name: "SuscripcionesNegocio");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_NegocioId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPlanId",
                table: "PlanSuscripcion");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Clientes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Clientes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Clientes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 727, DateTimeKind.Utc).AddTicks(9913));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 728, DateTimeKind.Utc).AddTicks(1413));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 728, DateTimeKind.Utc).AddTicks(1420));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 727, DateTimeKind.Utc).AddTicks(2550));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 727, DateTimeKind.Utc).AddTicks(3977));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 727, DateTimeKind.Utc).AddTicks(3986));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 15, 22, 48, 31, 727, DateTimeKind.Utc).AddTicks(3989));

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Clientes_ClienteId",
                table: "Turnos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
