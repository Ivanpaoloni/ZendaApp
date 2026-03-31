namespace Zenda.Core.DTOs;

public class BloqueoCreateDto
{
    public Guid PrestadorId { get; set; }
    public Guid SedeId { get; set; }
    public DateTime InicioLocal { get; set; } // Lo que manda el frontend (ej: "2026-04-15 14:00")
    public DateTime FinLocal { get; set; }
    public string? Motivo { get; set; }
}

public class BloqueoReadDto : BloqueoCreateDto
{
    public Guid Id { get; set; }
}