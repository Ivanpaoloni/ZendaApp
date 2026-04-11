using System.ComponentModel.DataAnnotations;
using Zenda.Core.Enums;

namespace Zenda.Core.DTOs
{
    public class TurnoCreateDto
    {
        [Required]
        public Guid PrestadorId { get; set; }
        [Required(ErrorMessage = "El servicio es obligatorio.")]
        public Guid ServicioId { get; set; }
        [Required]
        public DateTime Inicio { get; set; }

        // --- Datos del Invitado (MVP) ---
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string NombreClienteInvitado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        public string EmailClienteInvitado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
        public string TelefonoClienteInvitado { get; set; } = string.Empty;
    }
    
    public class TurnoReadDto
    {
        public Guid Id { get; set; }
        public DateTime FechaHoraInicioUtc { get; set; }
        public DateTime FechaHoraFinUtc { get; set; }

        public string NombreClienteInvitado { get; set; } = string.Empty;
        public string EmailClienteInvitado { get; set; } = string.Empty;
        public string TelefonoClienteInvitado { get; set; } = string.Empty; 
        public Guid PrestadorId { get; set; }
        public string PrestadorNombre { get; set; } = string.Empty;
        public EstadoTurnoEnum Estado { get; set; } = EstadoTurnoEnum.Pendiente;

        public Guid ServicioId { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public string SedeNombre { get; set; } = string.Empty; // para filtrado
    }
}