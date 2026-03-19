var builder = WebApplication.CreateBuilder(args);

// 1. REGISTRAR SERVICIOS
builder.Services.AddControllers(); // Esto es clave para que use Controllers
builder.Services.AddOpenApi();     // Swagger/OpenAPI

var app = builder.Build();

// 2. CONFIGURAR PIPELINE
if (app.Environment.IsDevelopment() || true) // Forzamos true para verlo en Render
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// 3. MAPEAR RUTAS
app.MapControllers(); // Esto busca automáticamente tus controladores

// Dejamos un health check simple aquí también por seguridad
app.MapGet("/health", () => new { Status = "Zenda is Live", Time = DateTime.UtcNow });

app.Run();