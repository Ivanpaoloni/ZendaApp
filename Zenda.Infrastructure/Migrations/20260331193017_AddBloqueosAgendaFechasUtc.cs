using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBloqueosAgendaFechasUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Inicio",
                table: "BloqueosAgenda",
                newName: "InicioUtc");

            migrationBuilder.RenameColumn(
                name: "Fin",
                table: "BloqueosAgenda",
                newName: "FinUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InicioUtc",
                table: "BloqueosAgenda",
                newName: "Inicio");

            migrationBuilder.RenameColumn(
                name: "FinUtc",
                table: "BloqueosAgenda",
                newName: "Fin");
        }
    }
}
