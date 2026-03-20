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
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "Neon-Database");

builder.Services.AddHealthChecksUI(setup =>
{
    // Detectamos si estamos en producción (Render)
    var isProd = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

    // Si es prod, usamos la URL completa. Si es local, usamos el path relativo.
    var endpoint = isProd
        ? "https://zendaapp.onrender.com/health-api"
        : "/health-api";

    setup.AddHealthCheckEndpoint("Zenda API", endpoint);
    setup.SetEvaluationTimeInSeconds(30);
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