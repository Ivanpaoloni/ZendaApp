namespace Zenda.Core.DTOs
{
    public class ClienteReadDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Notas { get; set; }
        public int CantidadTurnos { get; set; } // Útil para la UI después
    }
}
