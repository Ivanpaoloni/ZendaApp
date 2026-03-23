namespace Zenda.Core.DTOs
{
    public class DisponibilidadFechaDto
    {
        public DateTime Fecha { get; set; }
        public List<string> HorariosLibres { get; set; } = new();
    }
}