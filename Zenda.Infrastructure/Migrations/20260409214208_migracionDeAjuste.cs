using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class migracionDeAjuste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VentanaReservaDias",
                table: "Negocios",
                type: "integer",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "AnticipacionMinimaHoras",
                table: "Negocios",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_NegocioId",
                table: "Prestadores",
                column: "NegocioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestadores_Negocios_NegocioId",
                table: "Prestadores",
                column: "NegocioId",
                principalTable: "Negocios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prestadores_Negocios_NegocioId",
                table: "Prestadores");

            migrationBuilder.DropIndex(
                name: "IX_Prestadores_NegocioId",
                table: "Prestadores");

            migrationBuilder.AlterColumn<int>(
                name: "VentanaReservaDias",
                table: "Negocios",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 30);

            migrationBuilder.AlterColumn<int>(
                name: "AnticipacionMinimaHoras",
                table: "Negocios",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);
        }
    }
}
