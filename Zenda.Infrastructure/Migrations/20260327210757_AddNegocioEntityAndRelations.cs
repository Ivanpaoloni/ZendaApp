using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNegocioEntityAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prestadores_Sedes_SedeId1",
                table: "Prestadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Sedes_Negocio_NegocioId",
                table: "Sedes");

            migrationBuilder.DropIndex(
                name: "IX_Prestadores_SedeId1",
                table: "Prestadores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Negocio",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "SedeId1",
                table: "Prestadores");

            migrationBuilder.RenameTable(
                name: "Negocio",
                newName: "Negocios");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Negocios",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Negocios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Negocios",
                table: "Negocios",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sedes_Negocios_NegocioId",
                table: "Sedes",
                column: "NegocioId",
                principalTable: "Negocios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sedes_Negocios_NegocioId",
                table: "Sedes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Negocios",
                table: "Negocios");

            migrationBuilder.RenameTable(
                name: "Negocios",
                newName: "Negocio");

            migrationBuilder.AddColumn<Guid>(
                name: "SedeId1",
                table: "Prestadores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Negocio",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Negocio",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Negocio",
                table: "Negocio",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_SedeId1",
                table: "Prestadores",
                column: "SedeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestadores_Sedes_SedeId1",
                table: "Prestadores",
                column: "SedeId1",
                principalTable: "Sedes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sedes_Negocio_NegocioId",
                table: "Sedes",
                column: "NegocioId",
                principalTable: "Negocio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
