using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;
using Zenda.Core.Interfaces;
using Zenda.Infrastructure.Services;

namespace Zenda.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Base de Datos
        services.AddDbContext<ZendaDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            // Apagamos el validador dinámico para poder migrar tranquilos 
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
        // Mapeo de la Interfaz al Contexto real (Para que el resto de tu app pueda usar IZendaDbContext)
        services.AddScoped<IZendaDbContext>(provider => provider.GetRequiredService<ZendaDbContext>());

        // 2. Configuración de Emails (Resend)
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration["Resend:ApiKey"]!;
        });
        services.AddHttpClient<ResendClient>();
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IMercadoPagoService, MercadoPagoService>();
        services.AddScoped<IEmailService, ResendEmailService>();

        // 3. Hangfire y Tareas en Segundo Plano
        services.AddScoped<IJobService, HangfireJobService>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => 
            {
                options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
            }, new PostgreSqlStorageOptions 
            {
                // OPTIMIZACIÓN 1: Reducir la frecuencia de consulta (Polling)
                // Por defecto, Hangfire consulta la DB muy rápido. 15 segundos es perfecto para 
                // recordatorios de turnos sin asfixiar la base de datos.
                QueuePollInterval = TimeSpan.FromSeconds(15), 
                
                // OPTIMIZACIÓN 2: Limpieza de disco
                // Verifica los trabajos completados/expirados cada 1 hora en lugar de constantemente.
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                
                // Otras opciones recomendadas para estabilidad:
                PrepareSchemaIfNecessary = true
            }));

        // OPTIMIZACIÓN 3: Limitar los trabajadores (Workers)
        // Por defecto Hangfire usa Environment.ProcessorCount * 5 (lo que abre muchas conexiones).
        // 2 Workers son más que suficientes para ZendaApp antes del lanzamiento masivo.
        services.AddHangfireServer(options => 
        {
            options.WorkerCount = 2; 
        });

        return services;
    }
}
