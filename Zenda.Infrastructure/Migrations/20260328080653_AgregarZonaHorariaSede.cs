using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarZonaHorariaSede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZonaHorariaId",
                table: "Sedes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZonaHorariaId",
                table: "Sedes");
        }
    }
}
