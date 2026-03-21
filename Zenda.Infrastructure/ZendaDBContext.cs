using Microsoft.EntityFrameworkCore;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure
{
    public class ZendaDbContext : DbContext, IZendaDbContext
    {
        public ZendaDbContext(DbContextOptions<ZendaDbContext> options) : base(options) { }

        public DbSet<Prestador> Prestadores { get; set; }
        public DbSet<Disponibilidad> Disponibilidad { get; set; }
        public DbSet<Turno> Turnos { get; set; }
    }
}