using Microsoft.EntityFrameworkCore;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure
{
    public class ZendaDbContext : DbContext, IZendaDbContext
    {
        public ZendaDbContext(DbContextOptions<ZendaDbContext> options) : base(options) { }

        // Agregamos el Negocio como raíz
        public DbSet<Disponibilidad> Disponibilidad { get; set; }
        public DbSet<Negocio> Negocios { get; set; }
        public DbSet<Prestador> Prestadores { get; set; }
        public DbSet<Sede> Sedes { get; set; }
        public DbSet<Turno> Turnos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configuración de Negocio (El Tenant)
            modelBuilder.Entity<Negocio>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(n => n.Slug).IsRequired().HasMaxLength(120);

                // 1 Negocio -> N Sedes
                entity.HasMany(n => n.Sedes)
                      .WithOne(s => s.Negocio)
                      .HasForeignKey(s => s.NegocioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 2. Configuración de Sedes
            modelBuilder.Entity<Sede>(entity =>
            {
                // 1 Sede -> N Prestadores
                entity.HasMany(s => s.Prestadores)
                      .WithOne(p => p.Sede)
                      .HasForeignKey(p => p.SedeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. Prestadores
            modelBuilder.Entity<Prestador>(entity =>
            {
                entity.HasOne(p => p.Sede)
                      .WithMany(s => s.Prestadores)
                      .HasForeignKey(p => p.SedeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. Disponibilidad y Turnos
            modelBuilder.Entity<Disponibilidad>(entity =>
            {
                entity.HasOne(d => d.Prestador)
                      .WithMany(p => p.Horarios)
                      .HasForeignKey(d => d.PrestadorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Turno>(entity =>
            {
                entity.HasOne(t => t.Prestador)
                      .WithMany(p => p.Turnos)
                      .HasForeignKey(t => t.PrestadorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}