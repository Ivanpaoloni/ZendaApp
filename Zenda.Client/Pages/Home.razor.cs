using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using Zenda.Client.Services;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Client.Pages;

public partial class Home : ComponentBase
{
    // --- INYECCIONES DE DEPENDENCIAS ---
    [CascadingParameter] protected Task<AuthenticationState> AuthStateTask { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected NegocioClient _negocioService { get; set; } = default!;
    [Inject] protected TurnoClient _turnoService { get; set; } = default!;
    [Inject] protected PrestadorClient _prestadorClient { get; set; } = default!;
    [Inject] protected SedeClient _sedeService { get; set; } = default!;
    [Inject] protected ServicioClient _servicioClient { get; set; } = default!;
    [Inject] protected DisponibilidadClient _disponibilidadService { get; set; } = default!;

    // --- ESTADOS DEL DASHBOARD ---
    protected int sedesContador = 0;
    protected int serviciosContador = 0;
    protected int equipoActivo = 0;
    protected int turnosHoy = 0;
    protected int completadosHoy = 0;
    protected decimal ingresosProyectadosHoy = 0;

    protected string textoBotonCopiar = "Copiar Enlace";
    protected string iconoBotonCopiar = "content_copy";
    protected string linkReserva = "";
    protected string nombreUsuario = "Admin";
    protected string nombreNegocio = "ZendaApp";
    protected string ocupacion = "0%";

    protected List<TurnoReadDto> proximosTurnos = new();
    protected List<BloqueoReadDto> ausenciasHoy = new();
    protected bool cargando = true;

    // --- CICLO DE VIDA ---
    protected override async Task OnInitializedAsync()
    {
        // 1. Agarramos la sesión actual
        var authState = await AuthStateTask;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // 2. Buscamos el nombre del usuario
            var claimNombre = user.Claims.FirstOrDefault(c => c.Type == "given_name");

            if (claimNombre != null && !string.IsNullOrWhiteSpace(claimNombre.Value))
            {
                nombreUsuario = claimNombre.Value;
            }
            else
            {
                var emailClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(emailClaim) && emailClaim.Contains("@"))
                {
                    nombreUsuario = emailClaim.Split('@')[0];
                    nombreUsuario = char.ToUpper(nombreUsuario[0]) + nombreUsuario.Substring(1).ToLower();
                }
            }
        }

        try
        {
            var hoy = DateTime.Today;

            // Llamadas a la API
            var sedes = await _sedeService.GetAll();
            var prestadores = await _prestadorClient.GetAll();
            var categorias = await _servicioClient.GetCatalogo();
            var bloqueos = await _disponibilidadService.GetBloqueosDeHoy() ?? new();
            // Contadores para el Onboarding
            sedesContador = sedes?.Count ?? 0;
            equipoActivo = prestadores?.Count ?? 0;
            serviciosContador = categorias?.SelectMany(c => c.Servicios).Count() ?? 0;
            ausenciasHoy = bloqueos;

            if (equipoActivo > 0)
            {
                var turnos = await _turnoService.GetByFecha(hoy) ?? new List<TurnoReadDto>();

                turnosHoy = turnos.Count;
                completadosHoy = turnos.Count(t => t.Estado == EstadoTurnoEnum.Completado);

                if (turnos != null)
                {
                    // Solo ingresos válidos
                    ingresosProyectadosHoy = turnos
                        .Where(t => t.Estado != EstadoTurnoEnum.Cancelado)
                        .Sum(t => t.Precio);

                    var horaActualUtc = DateTime.UtcNow;
                    proximosTurnos = turnos
                        .Where(t => t.FechaHoraInicioUtc >= horaActualUtc)
                        .OrderBy(t => t.FechaHoraInicioUtc)
                        .Take(5)
                        .ToList();
                }
            }

            var negocio = await _negocioService.GetPerfilAsync();
            if (negocio != null)
            {
                linkReserva = $"{Nav.BaseUri}{negocio.Slug}";
                nombreNegocio = negocio.Nombre;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando dashboard: {ex.Message}");
        }
        finally
        {
            cargando = false;
        }
    }

    // --- MÉTODOS DE LA UI ---
    protected string CalcularTiempoFaltante(DateTime fechaUtc)
    {
        var diferencia = fechaUtc - DateTime.UtcNow;

        if (diferencia.TotalMinutes <= 0) return "¡Ahora!";
        if (diferencia.TotalMinutes < 60) return $"En {Math.Ceiling(diferencia.TotalMinutes)} min";

        var horas = Math.Floor(diferencia.TotalHours);
        var minutos = Math.Ceiling(diferencia.TotalMinutes % 60);

        if (minutos == 0) return $"En {horas} hs";
        return $"En {horas} hs y {minutos} min";
    }

    protected async Task CopiarLink()
    {
        if (string.IsNullOrEmpty(linkReserva)) return;

        await JS.InvokeVoidAsync("navigator.clipboard.writeText", linkReserva);

        textoBotonCopiar = "¡Copiado!";
        iconoBotonCopiar = "check_circle";
        StateHasChanged();

        await Task.Delay(2000);
        textoBotonCopiar = "Copiar Enlace";
        iconoBotonCopiar = "content_copy";
        StateHasChanged();
    }

    protected void AbrirWhatsApp(string telefono, string nombreCliente)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return;

        var numLimpio = new string(telefono.Where(char.IsDigit).ToArray());
        var mensaje = Uri.EscapeDataString($"¡Hola {nombreCliente}! Te escribo de {nombreNegocio ?? "la barbería/estética"}.");
        var url = $"https://wa.me/{numLimpio}?text={mensaje}";

        Nav.NavigateTo(url, forceLoad: true);
    }
}