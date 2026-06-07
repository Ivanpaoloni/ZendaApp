using System;
using Zenda.Core.Models;

namespace Zenda.Core.Entities
{
    // Solo hereda de BaseEntity (para Id, auditoría y soft delete). NO usamos ITenantEntity.
    public class Aviso : BaseEntity
    {
        public string Titulo { get; set; } = string.Empty;
        public string ContenidoHtml { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public bool Activo { get; set; } = false;
    }
}