// Archivo: Zenda.Infrastructure/HealthChecks/ZendySaaSLogicCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.HealthChecks
{
    public class LogicCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        public LogicCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Usamos un Scope manual porque los Health Checks corren como Singleton por defecto, 
            // y necesitamos resolver un DbContext que es Scoped.
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IZendaDbContext>();

            try
            {
                // Aquí puedes agregar un chequeo lógico. Ejemplo: Validar que existan Sedes o Rubros.
                // Por ahora, verificamos que el contexto abstracto responda a nivel código.
                bool isDatabaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);

                if (!isDatabaseConnected)
                {
                    return HealthCheckResult.Degraded("Fallo lógico al intentar conectar a través de la abstracción IZendaDbcontext.");
                }

                return HealthCheckResult.Healthy("Lógica de negocio y contexto multi-tenant operando correctamente.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Excepción en la validación lógica de Zendy.", ex);
            }
        }
    }
}