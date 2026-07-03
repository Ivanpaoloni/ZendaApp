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
        services.AddDbContext<ZendaDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // reintentos autom
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null
                    );
                });

            // supresión de advertencias de migración
            options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IZendaDbContext>(provider => provider.GetRequiredService<ZendaDbContext>());

        #region Resend
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration["Resend:ApiKey"]!;
        });
        services.AddHttpClient<ResendClient>();
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IMercadoPagoService, MercadoPagoService>();
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddScoped<IExternalCalendarAuthService, GoogleCalendarAuthService>();
        #endregion

        #region Hangfire
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
                QueuePollInterval = TimeSpan.FromSeconds(30), // frecuencia de consulta. 
                InvisibilityTimeout = TimeSpan.FromMinutes(5), // Evita reprocesamientos agresivos
                JobExpirationCheckInterval = TimeSpan.FromHours(1), // Limpieza de disco, verifica los trabajos completados/expirados
                PrepareSchemaIfNecessary = true // Otras opciones recomendadas para estabilidad
            }));

        // Limitar los Workers
        // Por defecto Hangfire usa Environment.ProcessorCount * 5 (lo que abre muchas conexiones).
        // 2 Workers son más que suficientes para ZendaApp antes del lanzamiento masivo.
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
        });
        #endregion

        // REGISTRO DE HEALTH CHECKS (Monitoreo de Infraestructura)
        services
            .AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "PostgreSQL Zendy",
                tags: new[] { "db", "core" }
                )
            .AddHangfire(
                options => { options.MinimumAvailableServers = 1; },
                "Hangfire Workers",
                null,
                new[] { "jobs" }
            )
            //  explícita para evitar bloqueos (403 Forbidden)
            .AddUrlGroup(options =>
            {
                options.AddUri(new Uri("https://api.mercadopago.com/v1/payment_methods"), uriOptions =>
                {
                    uriOptions.AddCustomHeader("Authorization", $"Bearer {configuration["MercadoPago:AccessToken"]}");
                    uriOptions.AddCustomHeader("User-Agent", "Zendy-HealthCheck/1.0");
                });
            }, name: "Mercado Pago API", tags: new[] { "external-api", "billing" })

            .AddCheck<LogicCheck>("Zendy Core Logic", tags: new[] { "business-logic" });

        services.AddHealthChecksUI(setup =>
        {
            var baseApiUrl = configuration["BaseApiUrl"] ?? "https://api.zendy.com.ar/";
            var healthEndpoint = $"{baseApiUrl.TrimEnd('/')}/health";

            setup.SetEvaluationTimeInSeconds(30);
        }).AddInMemoryStorage();

        return services;
    }
}
