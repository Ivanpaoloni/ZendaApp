using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore;
using Zenda.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ZendaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

#region Health Checks Configuration
// 1. Registro de los checks (Base de datos)
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "Neon-Database");

// 2. Configuración de la UI (SOLO UNA VEZ)
builder.Services.AddHealthChecksUI(setup =>
{
    // Al usar solo el path, la UI asume que es el mismo dominio que la web
    setup.AddHealthCheckEndpoint("Zenda API", "/health-api");
    setup.SetEvaluationTimeInSeconds(30); // En prod, 30 segundos está bien para no saturar
})
.AddInMemoryStorage();
#endregion

var app = builder.Build();

// swagger se ve siempre OJO prod
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

#region Health Checks Endpoints
app.MapHealthChecks("/health-api", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options => { options.UIPath = "/health-ui"; });
app.MapHealthChecks("/health");
#endregion

app.Run();