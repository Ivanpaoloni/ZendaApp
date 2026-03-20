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
builder.Services.AddHealthChecks().AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddHealthChecksUI().AddInMemoryStorage();
#endregion

// interfaz con almacenamiento en memoria
builder.Services.AddHealthChecksUI(setup =>
{
    setup.AddHealthCheckEndpoint("Zenda API", "/health-api");
    setup.SetEvaluationTimeInSeconds(10);
}).AddInMemoryStorage();

var app = builder.Build();

// swagger se ve siempre OJO prod
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