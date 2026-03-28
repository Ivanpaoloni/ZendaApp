using System.ComponentModel.DataAnnotations;

namespace Zenda.Core.DTOs
{
    public class TurnoCreateDto
    {
        [Required]
        public Guid PrestadorId { get; set; }

        [Required]
        public DateTime Inicio { get; set; } // Está perfecto mantenerlo simple para el front

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

        // Es buena práctica devolver estas fechas explícitamente como UTC
        public DateTime FechaHoraInicioUtc { get; set; }
        public DateTime FechaHoraFinUtc { get; set; }

        // Datos del cliente
        public string NombreClienteInvitado { get; set; } = string.Empty;
        public string EmailClienteInvitado { get; set; } = string.Empty;
        public string TelefonoClienteInvitado { get; set; } = string.Empty;

        // CORRECCIÓN: Tu entidad ahora usa un string 'Estado' en lugar del booleano 'EstaConfirmado'
        public string Estado { get; set; } = string.Empty;
    }
}