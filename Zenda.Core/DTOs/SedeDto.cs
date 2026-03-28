using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenda.Core.DTOs
{
    public class SedeReadDto
    {
        public Guid Id { get; set; }
        // Lo mantenemos por referencia, pero el front rara vez lo usará
        public Guid NegocioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string ZonaHorariaId { get; set; } = "America/Argentina/Buenos_Aires";
    }
    public class SedeCreateDto
    {
        [Required(ErrorMessage = "El nombre de la sede es obligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria para que los clientes sepan llegar.")]
        [StringLength(200, ErrorMessage = "La dirección es demasiado larga")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La zona horaria es necesaria para coordinar los turnos.")]
        public string ZonaHorariaId { get; set; } = "America/Argentina/Buenos_Aires";
    }
}
