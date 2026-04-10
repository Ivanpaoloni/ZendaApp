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
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Mapeo de la Interfaz al Contexto real (Para que el resto de tu app pueda usar IZendaDbContext)
        services.AddScoped<IZendaDbContext>(provider => provider.GetRequiredService<ZendaDbContext>());

        // 2. Configuración de Emails (Resend)
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration["Resend:ApiKey"]!;
        });
        services.AddHttpClient<ResendClient>();
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailService, ResendEmailService>();

        // 3. Hangfire y Tareas en Segundo Plano
        services.AddScoped<IJobService, HangfireJobService>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();

        return services;
    }
}