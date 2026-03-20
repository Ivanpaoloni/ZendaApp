using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenda.Core.Entities;

namespace Zenda.Infrastructure
{
    public class ZendaDbContext : DbContext
    {
        public ZendaDbContext(DbContextOptions<ZendaDbContext> options) : base(options) { }

        public DbSet<Prestador> Prestadores { get; set; }
        // Aquí agregaremos Turnos, Pacientes, etc., más adelante
    }
}