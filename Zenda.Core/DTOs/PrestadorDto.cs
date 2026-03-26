namespace Zenda.Core.DTOs
{

    // Este es el que usamos para recibir datos (POST/PUT)
    public class PrestadorCreateDto
    {
        public Guid SedeId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; } = 30;

        // Opcional: Si desde el front ya mandan la disponibilidad inicial
        // public List<DisponibilidadCreateDto> Horarios { get; set; } = new();
    }

    // Este es el que devolvemos (GET)
    public class PrestadorReadDto
    {
        public Guid Id { get; set; }
        public Guid SedeId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; }

        // Asumiendo que tenés un DTO de lectura para la disponibilidad
        public List<DisponibilidadReadDto> Horarios { get; set; } = new();
    }

    public class PrestadorUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; }

    }
}