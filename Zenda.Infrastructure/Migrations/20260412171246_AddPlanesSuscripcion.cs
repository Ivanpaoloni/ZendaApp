using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanesSuscripcion : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlanSuscripcionId",
                table: "Negocios",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PlanSuscripcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    MaxSedes = table.Column<int>(type: "integer", nullable: false),
                    MaxProfesionales = table.Column<int>(type: "integer", nullable: false),
                    HabilitaRecordatoriosHangfire = table.Column<bool>(type: "boolean", nullable: false),
                    HabilitaCajaAvanzada = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioMensual = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSuscripcion", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PlanSuscripcion",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "HabilitaCajaAvanzada", "HabilitaRecordatoriosHangfire", "IsDeleted", "MaxProfesionales", "MaxSedes", "Nombre", "PrecioMensual", "Slug", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 4, 12, 17, 12, 44, 504, DateTimeKind.Utc).AddTicks(2142), null, false, false, false, 1, 1, "Single", 0m, "single", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 4, 12, 17, 12, 44, 504, DateTimeKind.Utc).AddTicks(3518), null, false, true, false, 5, 2, "Business", 0m, "business", null, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 4, 12, 17, 12, 44, 504, DateTimeKind.Utc).AddTicks(3524), null, false, true, false, 50, 10, "Pro", 0m, "pro", null, null }
                });

            // 🎯 EL PARCHE CLAVE:
            // Seteamos el plan 'Single' a todos los negocios que ya existen 
            // antes de que Postgres intente validar la relación.
            migrationBuilder.Sql("UPDATE \"Negocios\" SET \"PlanSuscripcionId\" = '11111111-1111-1111-1111-111111111111';");

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 12, 17, 12, 44, 503, DateTimeKind.Utc).AddTicks(4911));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 12, 17, 12, 44, 503, DateTimeKind.Utc).AddTicks(6294));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 12, 17, 12, 44, 503, DateTimeKind.Utc).AddTicks(6303));

            migrationBuilder.UpdateData(
                table: "Rubros",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 12, 17, 12, 44, 503, DateTimeKind.Utc).AddTicks(6305));

            migrationBuilder.CreateIndex(
                name: "IX_Negocios_PlanSuscripcionId",
                table: "Negocios",
                column: "PlanSuscripcionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Negocios_PlanSuscripcion_PlanSuscripcionId",
                table: "Negocios",
                column: "PlanSuscripcionId",
                principalTable: "PlanSuscripcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // Cambiado a Restrict por seguridad
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Negocios_PlanSuscripcion_PlanSuscripcionId",
                table: "Negocios");

            migrationBuilder.DropTable(
                name: "PlanSuscripcion");

            migrationBuilder.DropIndex(
                name: "IX_Negocios_PlanSuscripcionId",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "PlanSuscripcionId",
                table: "Negocios");

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
    }
}
