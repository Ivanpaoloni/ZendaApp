using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Zenda.Core.Entities;

namespace Zenda.Core.Interfaces;

public interface IZendaDbContext
{
    DbSet<Negocio> Negocios { get; set; }
    DbSet<Cliente> Clientes { get; set; }
    DbSet<Sede> Sedes { get; set; }
    DbSet<Prestador> Prestadores { get; set; }
    DbSet<Disponibilidad> Disponibilidad { get; set; }
    DbSet<Turno> Turnos { get; set; }
    DatabaseFacade Database { get; }
    DbSet<CategoriaServicio> CategoriasServicio { get; set; }
    DbSet<Servicio> Servicios { get; set; }
    DbSet<BloqueoAgenda> BloqueosAgenda { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    // Agregamos FindAsync para que el Service lo use sin depender de la implementación
    ValueTask<TEntity?> FindAsync<TEntity>(params object?[]? keyValues) where TEntity : class;
}