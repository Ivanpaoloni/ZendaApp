namespace Zenda.Core.Entities;

public class Prestador
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // Ej: "dr-perez"
    public string Email { get; set; } = string.Empty;

    // Relación con horarios y turnos
    public List<Disponibilidad> Horarios { get; set; } = new();
    public List<Turno> Turnos { get; set; } = new();
}