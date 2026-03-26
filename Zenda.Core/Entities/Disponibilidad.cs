namespace Zenda.Core.Entities;

public class Disponibilidad
{
    public Guid Id { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    // Relación con el Prestador
    public Guid PrestadorId { get; set; }
    public Prestador? Prestador { get; set; }
}