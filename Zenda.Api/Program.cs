using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Zenda.Api.Middlewares;
using Zenda.API.Services;
using Zenda.Application.Services;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;
using Zenda.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Infraestructura (Base de datos, Correos, Hangfire)
// Una sola línea que carga toda la maquinaria pesada
builder.Services.AddInfrastructureServices(builder.Configuration);
#endregion

#region Identity y Seguridad
// Configurar Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ZendaDbContext>()
.AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "UnaClaveSuperSecretaYLargaParaZenda2026!@#");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,   // En prod idealmente van en true
        ValidateAudience = false, // En prod idealmente van en true
        RequireExpirationTime = true,
        ValidateLifetime = true
    };
});
#endregion

#region Servicios de Aplicación
// Habilitar el acceso al HttpContext para leer tokens/sesiones
builder.Services.AddHttpContextAccessor();

// Registro de los Servicios Core
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDisponibilidadService, DisponibilidadService>();
builder.Services.AddScoped<INegocioService, NegocioService>();
builder.Services.AddScoped<IPrestadoresService, PrestadoresService>();
builder.Services.AddScoped<ISedeService, SedeService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITurnosService, TurnosService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
#endregion

#region Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "Neon-Database");

builder.Services.AddHealthChecksUI().AddInMemoryStorage();
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7258",
            "https://zenda-frontend.onrender.com",
            "https://zendalanding.onrender.com"
        ).AllowAnyMethod().AllowAnyHeader();
    });
});
#endregion

var app = builder.Build();

// ==========================================
// PIPELINE DE MIDDLEWARES (El orden importa)
// ==========================================

// 1. Manejo global de excepciones
app.UseMiddleware<ExceptionMiddleware>();

// 2. Archivos estáticos
app.UseStaticFiles();

// 3. Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 4. Redirección HTTPS
app.UseHttpsRedirection();

// 5. CORS (Siempre antes de la autenticación)
app.UseCors("BlazorPolicy");

// 6. Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// 7. Hangfire Dashboard (Panel visual de tareas)
app.UseHangfireDashboard();

// 8. Controladores (Tus endpoints)
app.MapControllers();

// 9. Endpoints de Health Checks
app.UseHealthChecks("/health", new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
app.UseHealthChecksUI(config => config.UIPath = "/health-dash");

app.Run();