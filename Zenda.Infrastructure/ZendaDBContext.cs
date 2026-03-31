using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure
{
    // Cambiamos DbContext por IdentityDbContext<ApplicationUser>
    public class ZendaDbContext : IdentityDbContext<ApplicationUser>, IZendaDbContext
    {
        private readonly ITenantService _tenantService;
        public ZendaDbContext(DbContextOptions<ZendaDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }
        public Guid? CurrentTenantId => _tenantService.GetCurrentTenantId();
        public DbSet<Disponibilidad> Disponibilidad { get; set; }
        public DbSet<Negocio> Negocios { get; set; }
        public DbSet<Prestador> Prestadores { get; set; }
        public DbSet<Sede> Sedes { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<CategoriaServicio> CategoriasServicio { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<BloqueoAgenda> BloqueosAgenda { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ¡Esto siempre debe estar primero
            // Es lo que mapea las tablas de Identity (AspNetUsers, etc.)
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sede>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);
            modelBuilder.Entity<Prestador>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);
            modelBuilder.Entity<Turno>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);

            // Como Disponibilidad no tiene NegocioId directo, filtramos a través de su Prestador
            modelBuilder.Entity<Disponibilidad>().HasQueryFilter(e => CurrentTenantId == null || e.Prestador!.NegocioId == CurrentTenantId);

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

            modelBuilder.Entity<Sede>(entity =>
            {
                // 1 Sede -> N Prestadores
                entity.HasMany(s => s.Prestadores)
                      .WithOne(p => p.Sede)
                      .HasForeignKey(p => p.SedeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Prestador>(entity =>
            {
                entity.HasOne(p => p.Sede)
                      .WithMany(s => s.Prestadores)
                      .HasForeignKey(p => p.SedeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Disponibilidad>(entity =>
            {
                entity.HasOne(d => d.Prestador)
                      .WithMany(p => p.Horarios)
                      .HasForeignKey(d => d.PrestadorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Turno>(entity =>
            {
                // 1. Relación con Prestador (Lo que ya tenías)
                entity.HasOne(t => t.Prestador)
                      .WithMany(p => p.Turnos)
                      .HasForeignKey(t => t.PrestadorId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 2. Conversión de Enum a String (Lo nuevo)
                // Esto hace que en C# uses 'EstadoTurno' pero en SQL se lea "Pendiente", "Confirmado", etc.
                entity.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20); // Opcional, pero recomendado para performance
            });

            modelBuilder.Entity<CategoriaServicio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);

                // Un Negocio tiene muchas Categorías
                entity.HasMany(c => c.Servicios)
                      .WithOne(s => s.Categoria)
                      .HasForeignKey(s => s.CategoriaId)
                      .OnDelete(DeleteBehavior.Restrict); // No borramos servicios si se borra la categoría por error
            });

            modelBuilder.Entity<Servicio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);

                // Configuración para el precio (Sugerido para SQL Server/Postgres)
                entity.Property(e => e.Precio)
                      .HasPrecision(18, 2);

                entity.Property(e => e.DuracionMinutos).IsRequired();
            });
        }
    }
}