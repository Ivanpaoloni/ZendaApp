using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Zenda.Api.Middlewares;
using Zenda.Application.Services;
using Zenda.Core.Interfaces;
using Zenda.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Inyecciones
// Registro del Contexto con su Interfaz
builder.Services.AddDbContext<ZendaDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Mapeo de la Interfaz al Contexto real
builder.Services.AddScoped<IZendaDbContext>(provider => provider.GetRequiredService<ZendaDbContext>());

// Registro del Servicio
builder.Services.AddScoped<ITurnosService, TurnosService>();
builder.Services.AddScoped<IPrestadoresService, PrestadoresService>();
builder.Services.AddScoped<IDisponibilidadService, DisponibilidadService>();
builder.Services.AddScoped<ISedeService, SedeService>();

#endregion

#region Health Checks Configuration
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "Neon-Database");

builder.Services.AddHealthChecksUI().AddInMemoryStorage();
#endregion

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7258",
            "https://zenda-frontend.onrender.com"
        ).AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("BlazorPolicy");
// swagger se ve siempre OJO prod
//if (app.Environment.IsDevelopment())
//{dotnet add Zenda.Client/Zenda.Client.csproj reference Zenda.Core/Zenda.Core.csproj
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//middleware de manejo global de excepciones
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

#region Health Checks Endpoints

//HealthCheck Middleware
app.UseHealthChecks("/health", new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
app.UseHealthChecksUI(config => config.UIPath = "/health-dash");

#endregion

app.Run();