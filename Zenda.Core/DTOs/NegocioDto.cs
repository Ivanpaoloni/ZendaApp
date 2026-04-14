namespace Zenda.Core.DTOs;

public class NegocioReadDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public Guid RubroId { get; set; }
    public int IntervaloTurnosMinutos { get; set; }
    public string NombreRubro { get; set; } = string.Empty; 
    public int AnticipacionMinimaHoras { get; set; }
    public int VentanaReservaDias { get; set; }

    // Para mostrar info del plan actual sin necesidad de hacer otro endpoint o consulta extra
    public Guid PlanSuscripcionId { get; set; }
    public string PlanNombre { get; set; } = string.Empty;
    public int MaxProfesionales { get; set; }
    public int MaxSedes { get; set; }
}

public class NegocioCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; 
    public Guid PlanSuscripcionId { get; set; }
    public Guid RubroId { get; set; }
}

public class NegocioUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public Guid RubroId { get; set; }
    public int IntervaloTurnosMinutos { get; set; }
    public int AnticipacionMinimaHoras { get; set; }
}