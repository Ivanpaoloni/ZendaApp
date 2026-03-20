namespace Zenda.Core.Entities;

public class Disponibilidad
{
    public Guid Id { get; set; } // <--- ESTO ES LO QUE FALTA

    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    // Relación con el Prestador
    public Guid PrestadorId { get; set; }
}