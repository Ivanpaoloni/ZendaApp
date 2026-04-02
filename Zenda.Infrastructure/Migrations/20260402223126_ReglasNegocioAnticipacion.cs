using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReglasNegocioAnticipacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnticipacionMinimaHoras",
                table: "Negocios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VentanaReservaDias",
                table: "Negocios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnticipacionMinimaHoras",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "VentanaReservaDias",
                table: "Negocios");
        }
    }
}
