using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Zenda.Client;
using Zenda.Client.Auth;
using Zenda.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Definimos la URL de la API una sola vez
var apiUrl = builder.Configuration["BaseApiUrl"] ?? builder.HostEnvironment.BaseAddress;

// 2. Registramos el Interceptor (MessageHandler)
builder.Services.AddScoped<AuthMessageHandler>();

// 3. Registramos los Clientes Tipados
// IMPORTANTE: Al usar AddHttpClient, ya se registran automáticamente como Scoped.
// Todos estos van a usar el AuthMessageHandler para mandar el token.
builder.Services.AddHttpClient<NegocioClient>(client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<SedeClient>(client => client.BaseAddress = new Uri(apiUrl))
.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<PrestadorClient>(client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<TurnoClient>(client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<DisponibilidadClient>(client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<ServicioClient>(client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<AuthMessageHandler>();

// 4. Configuración de Auth y LocalStorage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// 5. Estado global de la App
builder.Services.AddScoped<AppState>();

// 6. HttpClient Genérico (OPCIONAL)
// Solo por si algún componente inyecta HttpClient directamente en lugar de un Client específico.
// Este NO lleva el interceptor (útil para llamadas públicas).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiUrl) });

var culture = new System.Globalization.CultureInfo("es-AR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

await builder.Build().RunAsync();