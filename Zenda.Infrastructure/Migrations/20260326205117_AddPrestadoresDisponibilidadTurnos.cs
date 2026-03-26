using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zenda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrestadoresDisponibilidadTurnos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Prestadores_PrestadorId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "Especialidad",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Prestadores");

            migrationBuilder.RenameColumn(
                name: "Inicio",
                table: "Turnos",
                newName: "FechaHoraInicioUtc");

            migrationBuilder.RenameColumn(
                name: "Fin",
                table: "Turnos",
                newName: "FechaHoraFinUtc");

            migrationBuilder.RenameColumn(
                name: "EstaConfirmado",
                table: "Turnos",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "ClienteNombre",
                table: "Turnos",
                newName: "TelefonoClienteInvitado");

            migrationBuilder.RenameColumn(
                name: "ClienteEmail",
                table: "Turnos",
                newName: "NombreClienteInvitado");

            migrationBuilder.AddColumn<string>(
                name: "ClienteUserId",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailClienteInvitado",
                table: "Turnos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Turnos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "NegocioId",
                table: "Turnos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Prestadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Prestadores",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Prestadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Prestadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "NegocioId",
                table: "Prestadores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SedeId",
                table: "Prestadores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SedeId1",
                table: "Prestadores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Prestadores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Prestadores",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Negocio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negocio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sedes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sedes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sedes_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_SedeId",
                table: "Prestadores",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_SedeId1",
                table: "Prestadores",
                column: "SedeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Sedes_NegocioId",
                table: "Sedes",
                column: "NegocioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestadores_Sedes_SedeId",
                table: "Prestadores",
                column: "SedeId",
                principalTable: "Sedes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prestadores_Sedes_SedeId1",
                table: "Prestadores",
                column: "SedeId1",
                principalTable: "Sedes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Prestadores_PrestadorId",
                table: "Turnos",
                column: "PrestadorId",
                principalTable: "Prestadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prestadores_Sedes_SedeId",
                table: "Prestadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestadores_Sedes_SedeId1",
                table: "Prestadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Prestadores_PrestadorId",
                table: "Turnos");

            migrationBuilder.DropTable(
                name: "Sedes");

            migrationBuilder.DropTable(
                name: "Negocio");

            migrationBuilder.DropIndex(
                name: "IX_Prestadores_SedeId",
                table: "Prestadores");

            migrationBuilder.DropIndex(
                name: "IX_Prestadores_SedeId1",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "ClienteUserId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "EmailClienteInvitado",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "NegocioId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "NegocioId",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "SedeId",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "SedeId1",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Prestadores");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Prestadores");

            migrationBuilder.RenameColumn(
                name: "TelefonoClienteInvitado",
                table: "Turnos",
                newName: "ClienteNombre");

            migrationBuilder.RenameColumn(
                name: "NombreClienteInvitado",
                table: "Turnos",
                newName: "ClienteEmail");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Turnos",
                newName: "EstaConfirmado");

            migrationBuilder.RenameColumn(
                name: "FechaHoraInicioUtc",
                table: "Turnos",
                newName: "Inicio");

            migrationBuilder.RenameColumn(
                name: "FechaHoraFinUtc",
                table: "Turnos",
                newName: "Fin");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Prestadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Especialidad",
                table: "Prestadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Prestadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Prestadores_PrestadorId",
                table: "Turnos",
                column: "PrestadorId",
                principalTable: "Prestadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
