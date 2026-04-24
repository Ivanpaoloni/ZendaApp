using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;
using Zenda.Core.Models;

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
        public DbSet<BloqueoAgenda> BloqueosAgenda { get; set; }
        public DbSet<CategoriaServicio> CategoriasServicio { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public Guid? CurrentTenantId => _tenantService.GetCurrentTenantId();
        public DbSet<Disponibilidad> Disponibilidad { get; set; }
        public DbSet<Negocio> Negocios { get; set; }
        public DbSet<Prestador> Prestadores { get; set; }
        public DbSet<Rubro> Rubros { get; set; }
        public DbSet<Sede> Sedes { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<SuscripcionNegocio> SuscripcionesNegocio { get; set; }
        public DbSet<HistorialPago> HistorialPagos { get; set; }
        public DbSet<PlanSuscripcion> PlanesSuscripcion { get; set; }
        public DbSet<CajaDiaria> CajasDiarias { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ¡Esto siempre debe estar primero!
            // Es lo que mapea las tablas de Identity (AspNetUsers, etc.)
            base.OnModelCreating(modelBuilder);

            // ==============================================
            // FILTROS GLOBALES (MULTI-TENANT & SOFT DELETE)
            // ==============================================
            modelBuilder.Entity<Sede>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);
            modelBuilder.Entity<Prestador>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);
            modelBuilder.Entity<Turno>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);
            modelBuilder.Entity<BloqueoAgenda>().HasQueryFilter(e => CurrentTenantId == null || e.Prestador!.NegocioId == CurrentTenantId);
            modelBuilder.Entity<CajaDiaria>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);
            modelBuilder.Entity<MovimientoCaja>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);

            // NUEVO: Filtro para que el Tenant solo vea sus propios clientes
            modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.NegocioId == CurrentTenantId);

            // Como Disponibilidad no tiene NegocioId directo, filtramos a través de su Prestador
            modelBuilder.Entity<Disponibilidad>().HasQueryFilter(e => CurrentTenantId == null || e.Prestador!.NegocioId == CurrentTenantId);

            // ==============================================
            // CONFIGURACIÓN DE ENTIDADES
            // ==============================================

            // --- NUEVO: Configuración de Cliente ---
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Email).HasMaxLength(150);
                entity.Property(c => c.Telefono).HasMaxLength(50);

                // Relación Cliente -> Negocio (Un cliente pertenece a un negocio)
                entity.HasOne<Negocio>()
                      .WithMany()
                      .HasForeignKey(c => c.NegocioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Turno>(entity =>
            {
                // 1. Relación con Prestador
                entity.HasOne(t => t.Prestador)
                      .WithMany(p => p.Turnos)
                      .HasForeignKey(t => t.PrestadorId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 2. NUEVO: Relación con Cliente
                entity.HasOne(t => t.Cliente)
                      .WithMany(c => c.Turnos)
                      .HasForeignKey(t => t.ClienteId)
                      .OnDelete(DeleteBehavior.Restrict); // Si borramos un cliente, los turnos se quedan (o podés usar Cascade si preferís borrar el historial)

                // 3. Conversión de Enum a String
                entity.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20);
            });

            modelBuilder.Entity<Rubro>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Codigo).IsRequired().HasMaxLength(20);

                // Dejamos los rubros fundacionales ya creados en la base de datos
                entity.HasData(
                    new Rubro { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Nombre = "Barbería", Codigo = "BARBERIA", Activo = true },
                    new Rubro { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Nombre = "Peluquería", Codigo = "PELUQUERIA", Activo = true },
                    new Rubro { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Nombre = "Centro de Estética", Codigo = "ESTETICA", Activo = true },
                    new Rubro { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Nombre = "Manicura y Pedicura", Codigo = "UNAS", Activo = true }
                );
            });

            var planFreeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            modelBuilder.Entity<PlanSuscripcion>(entity =>
            {
                entity.ToTable("PlanSuscripcion");
                entity.HasData(
                new PlanSuscripcion
                {
                    Id = planFreeId,
                    Nombre = "Single",
                    Slug = "single",
                    MaxSedes = 1,
                    MaxProfesionales = 1,
                    HabilitaRecordatoriosHangfire = false
                },
                new PlanSuscripcion
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Nombre = "Pro",
                    Slug = "pro",
                    MaxSedes = 1,
                    MaxProfesionales = 5,
                    HabilitaRecordatoriosHangfire = true
                },
                new PlanSuscripcion
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Nombre = "Business",
                    Slug = "business",
                    MaxSedes = 5,
                    MaxProfesionales = 25,
                    HabilitaRecordatoriosHangfire = true
                }
            );
            });


            modelBuilder.Entity<Negocio>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(n => n.Slug).IsRequired().HasMaxLength(120);

                entity.Property(n => n.AnticipacionMinimaHoras).HasDefaultValue(2);
                entity.Property(n => n.VentanaReservaDias).HasDefaultValue(30);
                entity.Property(n => n.IntervaloTurnosMinutos).HasDefaultValue(30);

                entity.HasOne(n => n.Rubro)
                      .WithMany(r => r.Negocios)
                      .HasForeignKey(n => n.RubroId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(n => n.Sedes)
                      .WithOne(s => s.Negocio)
                      .HasForeignKey(s => s.NegocioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Sede>(entity =>
            {
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

                entity.HasOne(p => p.Negocio)
                      .WithMany()
                      .HasForeignKey(p => p.NegocioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Disponibilidad>(entity =>
            {
                entity.HasOne(d => d.Prestador)
                      .WithMany(p => p.Horarios)
                      .HasForeignKey(d => d.PrestadorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CategoriaServicio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);

                entity.HasMany(c => c.Servicios)
                      .WithOne(s => s.Categoria)
                      .HasForeignKey(s => s.CategoriaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Servicio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Precio).HasPrecision(18, 2);
                entity.Property(e => e.DuracionMinutos).IsRequired();
            });

            modelBuilder.Entity<SuscripcionNegocio>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Convertir el Enum a String en la BD
                entity.Property(s => s.Estado)
                      .HasConversion<string>()
                      .HasMaxLength(30);

                entity.Property(s => s.MercadoPagoPreapprovalId).HasMaxLength(150);

                // Relación: Un Negocio tiene una Suscripción (o historial de ellas)
                entity.HasOne(s => s.Negocio)
                      .WithMany() // Si decides que Negocio tenga un ICollection<SuscripcionNegocio>, ponlo aquí
                      .HasForeignKey(s => s.NegocioId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relación: La Suscripción pertenece a un Plan
                entity.HasOne(s => s.PlanSuscripcion)
                      .WithMany()
                      .HasForeignKey(s => s.PlanSuscripcionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HistorialPago>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.MontoCobrado).HasPrecision(18, 2);
                entity.Property(h => h.MercadoPagoPaymentId).HasMaxLength(150);

                // Relación: Un Historial de Pago pertenece a un contrato de Suscripción
                entity.HasOne(h => h.SuscripcionNegocio)
                      .WithMany() // Opcional: .WithMany(s => s.Pagos) si lo agregas a la entidad
                      .HasForeignKey(h => h.SuscripcionNegocioId)
                      .OnDelete(DeleteBehavior.Cascade); // Si se borra la suscripción, se borra su historial
            });

            // --- CONFIGURACIÓN DE CAJA ---
            modelBuilder.Entity<CajaDiaria>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.MontoInicial).HasPrecision(18, 2);
                entity.Property(c => c.MontoFinalDeclarado).HasPrecision(18, 2);

                entity.HasOne(c => c.Sede)
                      .WithMany()
                      .HasForeignKey(c => c.SedeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MovimientoCaja>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Monto).HasPrecision(18, 2);
                entity.Property(m => m.Detalle).HasMaxLength(255);

                entity.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
                entity.Property(m => m.MedioPago).HasConversion<string>().HasMaxLength(30);

                entity.HasOne(m => m.CajaDiaria)
                      .WithMany(c => c.Movimientos)
                      .HasForeignKey(m => m.CajaDiariaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Turno)
                      .WithMany() // Un turno podría tener múltiples pagos (ej: mitad efectivo, mitad MP)
                      .HasForeignKey(m => m.TurnoId)
                      .OnDelete(DeleteBehavior.SetNull); // Si se borra el turno (hard delete), el pago queda en caja para la AFIP/Contabilidad
            });
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.Id == Guid.Empty)
                            entry.Entity.Id = Guid.NewGuid();

                        entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}