using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CajaDiaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CajasDiarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    SedeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MontoInicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoFinalDeclarado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstaAbierta = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajasDiarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CajasDiarias_Sedes_SedeId",
                        column: x => x.SedeId,
                        principalTable: "Sedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCaja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CajaDiariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MedioPago = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCaja_CajasDiarias_CajaDiariaId",
                        column: x => x.CajaDiariaId,
                        principalTable: "CajasDiarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientosCaja_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                columns: new[] { "CreatedAtUtc", "MaxSedes", "Nombre", "Slug" },
                values: new object[] { new DateTime(2026, 4, 24, 14, 58, 58, 631, DateTimeKind.Utc).AddTicks(610), 1, "Pro", "pro" });

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAtUtc", "MaxProfesionales", "MaxSedes", "Nombre", "Slug" },
                values: new object[] { new DateTime(2026, 4, 24, 14, 58, 58, 631, DateTimeKind.Utc).AddTicks(616), 25, 5, "Business", "business" });

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

            migrationBuilder.CreateIndex(
                name: "IX_CajasDiarias_SedeId",
                table: "CajasDiarias",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_CajaDiariaId",
                table: "MovimientosCaja",
                column: "CajaDiariaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_TurnoId",
                table: "MovimientosCaja",
                column: "TurnoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "CajasDiarias");

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 21, 3, 25, 33, 755, DateTimeKind.Utc).AddTicks(298));

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAtUtc", "MaxSedes", "Nombre", "Slug" },
                values: new object[] { new DateTime(2026, 4, 21, 3, 25, 33, 755, DateTimeKind.Utc).AddTicks(1706), 2, "Business", "business" });

            migrationBuilder.UpdateData(
                table: "PlanSuscripcion",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAtUtc", "MaxProfesionales", "MaxSedes", "Nombre", "Slug" },
                values: new object[] { new DateTime(2026, 4, 21, 3, 25, 33, 755, DateTimeKind.Utc).AddTicks(1711), 50, 10, "Pro", "pro" });

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
        }
    }
}
