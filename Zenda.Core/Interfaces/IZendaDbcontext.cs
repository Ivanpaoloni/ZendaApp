using Microsoft.EntityFrameworkCore;
using Zenda.Core.Entities;

namespace Zenda.Core.Interfaces;

public interface IZendaDbContext
{
    DbSet<Negocio> Negocios { get; set; } // <-- Agregar esto
    DbSet<Sede> Sedes { get; set; }
    DbSet<Prestador> Prestadores { get; set; }
    DbSet<Disponibilidad> Disponibilidad { get; set; }
    DbSet<Turno> Turnos { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    // Agregamos FindAsync para que el Service lo use sin depender de la implementación
    ValueTask<TEntity?> FindAsync<TEntity>(params object?[]? keyValues) where TEntity : class;
}