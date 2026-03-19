namespace Zenda.Core.Entities;

public class Turno
{
    public Guid Id { get; set; }
    public Guid PrestadorId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public bool EstaConfirmado { get; set; } = false;
}