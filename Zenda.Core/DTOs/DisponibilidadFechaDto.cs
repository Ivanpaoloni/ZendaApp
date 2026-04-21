namespace Zenda.Core.DTOs
{
    public class DisponibilidadFechaDto
    {
        public DateTime Fecha { get; set; }
        public List<HorarioDisponibleDto> HorariosLibres { get; set; } = new();
    }

    public class HorarioDisponibleDto
    {
        public string Hora { get; set; } = string.Empty;
        public Guid PrestadorId { get; set; }
    }
}