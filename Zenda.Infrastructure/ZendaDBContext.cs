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
        public DbSet<Sede> Sedes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones y restricciones
            modelBuilder.Entity<Prestador>(entity =>
            {
                // 1 Sede -> N Prestadores
                entity.HasOne(p => p.Sede)
                      .WithMany()
                      .HasForeignKey(p => p.SedeId)
                      .OnDelete(DeleteBehavior.Restrict); // Evita borrar una sede con barberos activos
            });

            modelBuilder.Entity<Disponibilidad>(entity =>
            {
                // 1 Prestador -> N Horarios
                entity.HasOne(d => d.Prestador)
                      .WithMany(p => p.Horarios)
                      .HasForeignKey(d => d.PrestadorId)
                      .OnDelete(DeleteBehavior.Cascade); // Si se borra el barbero, se borran sus horarios
            });

            modelBuilder.Entity<Turno>(entity =>
            {
                // 1 Prestador -> N Turnos
                entity.HasOne(t => t.Prestador)
                      .WithMany(p => p.Turnos)
                      .HasForeignKey(t => t.PrestadorId)
                      .OnDelete(DeleteBehavior.Restrict); // Protege el historial: no podés borrar un barbero con turnos
            });
        }
    }
}