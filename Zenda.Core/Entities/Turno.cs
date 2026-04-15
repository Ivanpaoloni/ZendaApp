using Zenda.Core.Enums;
using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Turno : BaseEntity, ITenantEntity
{
    public Guid NegocioId { get; set; }
    public Guid PrestadorId { get; set; }
    public Prestador? Prestador { get; set; }

    public DateTime FechaHoraInicioUtc { get; set; }
    public DateTime FechaHoraFinUtc { get; set; }

    // --- NUEVA RELACIÓN: EL CLIENTE ---
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public string? ClienteUserId { get; set; } // Lo dejamos para el futuro B2B2C como tenías pensado

    public EstadoTurnoEnum Estado { get; set; } = EstadoTurnoEnum.Pendiente;
    public Guid ServicioId { get; set; }
    public string? RecordatorioJobId { get; set; }
    public Servicio Servicio { get; set; } = null!;
}