using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;
using Zenda.Core.Interfaces;
using Zenda.Infrastructure.HealthChecks;
using Zenda.Infrastructure.Services;

namespace Zenda.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Base de Datos con Estrategia de Reintentos
        services.AddDbContext<ZendaDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // 🔥 LA SOLUCIÓN: Habilita reintentos automáticos para fallos transitorios
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,            // Máximo de reintentos
                        maxRetryDelay: TimeSpan.FromSeconds(30), // Tiempo máximo entre reintentos
                        errorCodesToAdd: null        // Códigos de error adicionales si fueran necesarios
                    );
                });

            // Mantenemos la supresión de advertencias de migración
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
        services.AddScoped<IExternalCalendarAuthService, GoogleCalendarAuthService>();

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
                QueuePollInterval = TimeSpan.FromSeconds(60), // Incluso 30s es seguro para recordatorios
                InvisibilityTimeout = TimeSpan.FromMinutes(5), // Evita re-procesamientos agresivos
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

        // ==========================================================
        // 4. REGISTRO DE HEALTH CHECKS (Monitoreo de Infraestructura)
        // ==========================================================
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!,
                name: "PostgreSQL Zendy", tags: new[] { "db", "core" })

            // 2. LA SOLUCIÓN: Pasamos los argumentos por posición para evitar el error de overload
            .AddHangfire(
                options => { options.MinimumAvailableServers = 1; }, // setup
                "Hangfire Workers",                                  // name (por posición)
                null,                                                // failureStatus
                new[] { "jobs" }                                     // tags
            )

            .AddUrlGroup(new Uri("https://api.mercadopago.com/v1/payments"),
                name: "Mercado Pago API", tags: new[] { "external-api", "billing" })

            .AddUrlGroup(new Uri("https://api.resend.com/emails"),
                name: "Resend API", tags: new[] { "external-api", "communications" })

            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 400,
                name: "Consumo de Memoria API", tags: new[] { "hosting", "resources" })

            .AddCheck<LogicCheck>("Zendy Core Logic", tags: new[] { "business-logic" });

        services.AddHealthChecksUI(setup =>
        {
            setup.AddHealthCheckEndpoint("Zendy API", "/health");
            setup.SetEvaluationTimeInSeconds(30);
        }).AddInMemoryStorage();
        return services;
    }
}
