using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Prestador : BaseEntity, ITenantEntity
{
    public Guid NegocioId { get; set; }
    public Guid SedeId { get; set; }
    public Sede? Sede { get; set; }
    public Negocio? Negocio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionTurnoMinutos { get; set; } = 30;

    // Si el día de mañana el barbero quiere entrar con su email, guardamos su ID de usuario acá.
    // Por ahora (MVP), puede ser null y el dueño le maneja la agenda.
    public string? ApplicationUserId { get; set; }

    public List<Disponibilidad> Horarios { get; set; } = new();
    public List<Turno> Turnos { get; set; } = new();
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}