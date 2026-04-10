using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using System.Text;
using Zenda.Api.Middlewares;
using Zenda.API.Services;
using Zenda.Application.Services;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;
using Zenda.Infrastructure;
using Zenda.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Inyecciones
// Registro del Contexto con su Interfaz
builder.Services.AddDbContext<ZendaDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configurar Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Acá podés relajar o endurecer las reglas de las contraseñas
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

// Mapeo de la Interfaz al Contexto real
builder.Services.AddScoped<IZendaDbContext>(provider => provider.GetRequiredService<ZendaDbContext>());

// 1. Habilitar el acceso al HttpContext para leer tokens/sesiones
builder.Services.AddHttpContextAccessor();

// Registro del Servicio
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

#region emailing
// Configuración de Resend
builder.Services.AddOptions();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiKey"]!;
});
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<IResend, ResendClient>();

// Tu servicio de correos
builder.Services.AddScoped<IEmailService, ResendEmailService>();
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
            "https://zenda-frontend.onrender.com",
            "https://zendalanding.onrender.com"
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
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication(); // Primero verifica QUIÉN es el usuario (lee el token)
app.UseAuthorization();  // Después verifica QUÉ puede hacer (roles)

app.MapControllers();

#region Health Checks Endpoints

//HealthCheck Middleware
app.UseHealthChecks("/health", new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
app.UseHealthChecksUI(config => config.UIPath = "/health-dash");

#endregion

app.Run();