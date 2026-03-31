using Microsoft.AspNetCore.Components;
using Zenda.Client.Services;
using Zenda.Core.DTOs;

namespace Zenda.Client.Pages.Public;

public partial class Reserva : ComponentBase
{
    [Parameter] public string NegocioSlug { get; set; } = string.Empty;

    // --- INYECCIONES ---
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected PrestadorClient _prestadorService { get; set; } = default!;
    [Inject] protected SedeClient _sedeService { get; set; } = default!;
    [Inject] protected NegocioClient _negocioClient { get; set; } = default!;
    [Inject] protected ServicioClient _servicioClient { get; set; } = default!;
    [Inject] protected TurnoClient TurnoService { get; set; } = default!;

    // --- ENUM Y ESTADO ---
    protected enum PasoReserva { Cargando, SeleccionarSede, SeleccionarServicio, SeleccionarPrestador, SeleccionarTurno, NoEncontrado }
    protected PasoReserva pasoActual = PasoReserva.Cargando;

    protected DateTime fechaSeleccionada = DateTime.Today;
    protected List<string> slots = new();
    protected string? horaSeleccionada;
    protected bool cargandoHorarios = false;
    protected bool enviandoReserva = false;
    protected TurnoCreateDto nuevoTurno = new();

    // --- MODELOS DE DATOS ---
    protected NegocioReadDto? negocio;
    protected List<SedeReadDto> sedesDisponibles = new();
    protected List<PrestadorReadDto> prestadoresDeSede = new();
    protected List<ServicioPublicoDto> serviciosDeSede = new();
    protected List<PrestadorReadDto> prestadoresFiltrados = new();

    // --- SELECCIONES ---
    protected SedeReadDto? sedeSeleccionada;
    protected ServicioPublicoDto? servicioSeleccionado;
    protected PrestadorReadDto? prestadorSeleccionado;

    protected override async Task OnParametersSetAsync()
    {
        // Usamos OnParametersSetAsync por si cambia el Slug en la misma ventana
        await CargarDatosIniciales();
    }

    private async Task CargarDatosIniciales()
    {
        try
        {
            pasoActual = PasoReserva.Cargando;

            negocio = await _negocioClient.GetPublicBySlugAsync(NegocioSlug);

            if (negocio == null)
            {
                pasoActual = PasoReserva.NoEncontrado;
                return;
            }

            sedesDisponibles = await _sedeService.GetPublicByNegocio(negocio.Id);

            if (sedesDisponibles.Count == 1)
            {
                SeleccionarSede(sedesDisponibles.First());
            }
            else if (sedesDisponibles.Count > 1)
            {
                pasoActual = PasoReserva.SeleccionarSede;
            }
            else
            {
                // No tiene sedes
                pasoActual = PasoReserva.SeleccionarSede;
            }
        }
        catch
        {
            negocio = null;
            pasoActual = PasoReserva.NoEncontrado;
        }
    }

    protected async void SeleccionarSede(SedeReadDto sede)
    {
        sedeSeleccionada = sede;
        pasoActual = PasoReserva.Cargando;
        StateHasChanged();

        var taskServicios = _servicioClient.GetServiciosPublicosPorSede(sede.Id);
        var taskPrestadores = _prestadorService.GetPublicBySede(sede.Id);

        await Task.WhenAll(taskServicios, taskPrestadores);

        serviciosDeSede = taskServicios.Result ?? new();
        prestadoresDeSede = taskPrestadores.Result ?? new();

        pasoActual = PasoReserva.SeleccionarServicio;
        StateHasChanged();
    }

    protected void SeleccionarServicio(ServicioPublicoDto servicio)
    {
        servicioSeleccionado = servicio;

        prestadoresFiltrados = prestadoresDeSede
            .Where(p => p.Servicios != null && p.Servicios.Any(s => s.Id == servicio.Id))
            .ToList();

        pasoActual = PasoReserva.SeleccionarPrestador;
    }

    protected async Task SeleccionarPrestador(PrestadorReadDto? prestador)
    {
        prestadorSeleccionado = prestador;
        pasoActual = PasoReserva.SeleccionarTurno;
        await CargarDisponibilidad();
    }

    // --- NAVEGACIÓN HACIA ATRÁS ---
    protected void VolverASedes() => pasoActual = PasoReserva.SeleccionarSede;
    protected void VolverAServicios() => pasoActual = PasoReserva.SeleccionarServicio;
    protected void VolverAPrestadores() => pasoActual = PasoReserva.SeleccionarPrestador;

    protected string ObtenerTextoPaso()
    {
        return pasoActual switch
        {
            PasoReserva.SeleccionarSede => "1. Elegí la sucursal",
            PasoReserva.SeleccionarServicio => "2. ¿Qué te querés hacer?",
            PasoReserva.SeleccionarPrestador => "3. Elegí tu profesional",
            PasoReserva.SeleccionarTurno => "4. Seleccioná día y hora",
            _ => "Preparando agenda..."
        };
    }

    protected async Task CargarDisponibilidad()
    {
        cargandoHorarios = true;
        slots.Clear();
        horaSeleccionada = null;

        try
        {
            if (prestadorSeleccionado != null)
            {
                var res = await TurnoService.GetDisponibilidad(prestadorSeleccionado.Id, fechaSeleccionada, servicioSeleccionado!.Id);
                slots = res?.HorariosLibres ?? new();
            }
            else
            {
                var primerPrestador = prestadoresFiltrados.FirstOrDefault();
                if (primerPrestador != null)
                {
                    // 🎯 BUG FIX: Usar primerPrestador.Id en lugar de prestadorSeleccionado!.Id
                    var res = await TurnoService.GetDisponibilidad(primerPrestador.Id, fechaSeleccionada, servicioSeleccionado!.Id);
                    slots = res?.HorariosLibres ?? new();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando disponibilidad: {ex.Message}");
        }
        finally
        {
            cargandoHorarios = false;
        }
    }

    protected void SeleccionarHora(string hora) => horaSeleccionada = hora;

    protected async Task ConfirmarReserva()
    {
        if (string.IsNullOrEmpty(nuevoTurno.NombreClienteInvitado) || string.IsNullOrEmpty(nuevoTurno.TelefonoClienteInvitado))
            return;

        enviandoReserva = true;
        try
        {
            Guid idPrestadorFinal;
            string nombrePrestadorReserva;

            if (prestadorSeleccionado != null)
            {
                idPrestadorFinal = prestadorSeleccionado.Id;
                nombrePrestadorReserva = prestadorSeleccionado.Nombre;
            }
            else
            {
                var asignado = prestadoresFiltrados.First();
                idPrestadorFinal = asignado.Id;
                nombrePrestadorReserva = asignado.Nombre;
            }

            var h = TimeOnly.Parse(horaSeleccionada!);
            var fechaLocal = fechaSeleccionada.Date.Add(h.ToTimeSpan());
            var fechaCruda = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);

            var dtoParaEnviar = new TurnoCreateDto
            {
                PrestadorId = idPrestadorFinal,
                ServicioId = servicioSeleccionado!.Id,
                NombreClienteInvitado = nuevoTurno.NombreClienteInvitado,
                TelefonoClienteInvitado = nuevoTurno.TelefonoClienteInvitado,
                EmailClienteInvitado = nuevoTurno.EmailClienteInvitado,
                Inicio = fechaCruda
            };

            var resultado = await TurnoService.Reservar(dtoParaEnviar);

            if (resultado != null)
            {
                var fechaEnc = Uri.EscapeDataString(resultado.FechaHoraInicioUtc.ToLocalTime().ToString("dd/MM/yyyy"));
                var horaEnc = Uri.EscapeDataString(resultado.FechaHoraInicioUtc.ToLocalTime().ToString("HH:mm"));
                var nombreEnc = Uri.EscapeDataString(nombrePrestadorReserva);
                var direccionEnc = Uri.EscapeDataString(sedeSeleccionada?.Direccion ?? "");
                var duracionReal = servicioSeleccionado?.DuracionMinutos ?? 30;

                Nav.NavigateTo($"/reserva-confirmada?fecha={fechaEnc}&hora={horaEnc}&nombre={nombreEnc}&duracion={duracionReal}&direccion={direccionEnc}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error crítico reservando: {ex.Message}");
        }
        finally
        {
            enviandoReserva = false;
        }
    }
}