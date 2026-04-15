using System.ComponentModel.DataAnnotations;
using Zenda.Core.Entities;

namespace Zenda.Core.DTOs
{

    // Este es el que usamos para recibir datos (POST/PUT)
    public class PrestadorCreateDto
    {
        public Guid SedeId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; } = 30;
        public List<Guid> ServiciosIds { get; set; } = new();
        // Opcional: Si desde el front ya mandan la disponibilidad inicial
        // public List<DisponibilidadCreateDto> Horarios { get; set; } = new();
    }

    public class PrestadorReadDto
    {
        public Guid Id { get; set; }
        public Guid SedeId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int DuracionTurnoMinutos { get; set; }


        public string SedeNombre { get; set; } = string.Empty;
        public List<DisponibilidadReadDto> Horarios { get; set; } = new();
        public List<ServicioReadDto> Servicios { get; set; } = new();
    }

    public class PrestadorUpdateDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 3)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes asignar una sede")]
        public Guid SedeId { get; set; }

        [Range(1, 480, ErrorMessage = "La duración debe ser entre 1 y 480 minutos")]
        public int DuracionTurnoMinutos { get; set; }
        public List<Guid> ServiciosIds { get; set; } = new();
    }
}