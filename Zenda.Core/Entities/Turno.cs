using Zenda.Core.Enums;
using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Turno : BaseEntity, ITenantEntity
{
    // Aislamiento: Este turno es de esta barbería
    public Guid NegocioId { get; set; }

    // ¿Quién atiende?
    public Guid PrestadorId { get; set; }
    public Prestador? Prestador { get; set; }

    // Fecha y hora (¡Siempre en UTC en la base de datos!)
    public DateTime FechaHoraInicioUtc { get; set; }
    public DateTime FechaHoraFinUtc { get; set; }

    // --- EL CLIENTE (El consumidor B2C) ---
    // Opción A: Es un usuario registrado en todo Zenda (Futuro B2B2C)
    public string? ClienteUserId { get; set; }

    // Opción B: Es un invitado (MVP: Dejó sus datos en la landing pública de la barbería)
    public string NombreClienteInvitado { get; set; } = string.Empty;
    public string TelefonoClienteInvitado { get; set; } = string.Empty;
    public string EmailClienteInvitado { get; set; } = string.Empty;
    public EstadoTurnoEnum Estado { get; set; } = EstadoTurnoEnum.Pendiente;
    public Guid ServicioId { get; set; }
    public string? RecordatorioJobId { get; set; }
    public Servicio Servicio { get; set; } = null!;
}