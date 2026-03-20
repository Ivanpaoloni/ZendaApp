namespace Zenda.Api.DTOs;

public class DisponibilidadCreateDto
{
    public int DiaSemana { get; set; } // 0=Domingo, 1=Lunes, etc.
    public string HoraInicio { get; set; } = string.Empty; // "09:00"
    public string HoraFin { get; set; } = string.Empty;    // "13:00"
    public Guid PrestadorId { get; set; }
}

public class DisponibilidadReadDto
{
    public Guid Id { get; set; }
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
}