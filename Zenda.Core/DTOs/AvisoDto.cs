using System;

namespace Zenda.Core.DTOs
{
    public class AvisoDto
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string ContenidoHtml { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool Activo { get; set; }
    }
}