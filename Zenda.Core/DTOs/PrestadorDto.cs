namespace Zenda.Core.DTOs
{

    // Este es el que usamos para recibir datos (POST/PUT)
    public class PrestadorCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; } = 30; // Valor por defecto
    }

    // Este es el que devolvemos (GET)
    public class PrestadorReadDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; } = 30; // Valor por defecto
        public List<DisponibilidadReadDto> Disponibilidad { get; set; } = new();
    }

    public class PrestadorUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; } = 30; // Valor por defecto

    }
}