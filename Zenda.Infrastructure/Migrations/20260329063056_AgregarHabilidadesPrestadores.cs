using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHabilidadesPrestadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrestadorServicio",
                columns: table => new
                {
                    PrestadoresId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiciosId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrestadorServicio", x => new { x.PrestadoresId, x.ServiciosId });
                    table.ForeignKey(
                        name: "FK_PrestadorServicio_Prestadores_PrestadoresId",
                        column: x => x.PrestadoresId,
                        principalTable: "Prestadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrestadorServicio_Servicios_ServiciosId",
                        column: x => x.ServiciosId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrestadorServicio_ServiciosId",
                table: "PrestadorServicio",
                column: "ServiciosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrestadorServicio");
        }
    }
}
