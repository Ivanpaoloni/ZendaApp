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
        public Guid NegocioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        // Opcional: Podrías querer saber cuántos prestadores hay en esta sede
        // public int CantidadPrestadores { get; set; }
    }
    public class SedeCreateDto
    {
        [Required(ErrorMessage = "El nombre de la sede es obligatorio")]
        [StringLength(100, MinimumLength = 3)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria para que los clientes sepan llegar.")]
        [StringLength(200)]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        public Guid NegocioId { get; set; }
    }
}
