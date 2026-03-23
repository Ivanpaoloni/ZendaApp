namespace Zenda.Core.DTOs
{
    public class DisponibilidadCreateDto
    {
        public Guid PrestadorId { get; set; }
        public int DiaSemana { get; set; }
        public TimeOnly HoraInicio { get; set; } // .NET Core 6+ ya sabe deserializar esto desde JSON "HH:mm"
        public TimeOnly HoraFin { get; set; }
    }

    public class DisponibilidadReadDto
    {
        public Guid Id { get; set; }
        public int DiaSemana { get; set; }
        public string HoraInicio { get; set; } = string.Empty;
        public string HoraFin { get; set; } = string.Empty;
    }
}