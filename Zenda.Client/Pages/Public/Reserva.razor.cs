using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zenda.Client.Services;
using Zenda.Core.DTOs;

namespace Zenda.Client.Pages.Public; // 👈 Confirmá que sea este tu namespace

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
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    // --- ENUM Y ESTADO ---
    protected enum PasoReserva { Cargando, SeleccionarSede, SeleccionarServicio, SeleccionarPrestador, SeleccionarTurno, CompletarDatos, NoEncontrado }
    protected PasoReserva pasoActual = PasoReserva.Cargando;
    protected string errorReserva = string.Empty;

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
    protected Dictionary<string, Guid> prestadoresPorHoraLibre = new();

    // --- SELECCIONES ---
    protected SedeReadDto? sedeSeleccionada;
    protected ServicioPublicoDto? servicioSeleccionado;
    protected PrestadorReadDto? prestadorSeleccionado;

    protected override async Task OnParametersSetAsync()
    {
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
                await JS.InvokeVoidAsync("cambiarFavicon", "");
                return;
            }
            if (!string.IsNullOrEmpty(negocio.LogoUrl))
            {
                await JS.InvokeVoidAsync("cambiarFavicon", negocio.LogoUrl);
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

    protected void SeleccionarHora(string hora)
    {
        horaSeleccionada = hora;
        pasoActual = PasoReserva.CompletarDatos;
    }

    protected void VolverASedes() => pasoActual = PasoReserva.SeleccionarSede;
    protected void VolverAServicios() => pasoActual = PasoReserva.SeleccionarServicio;
    protected void VolverAPrestadores() => pasoActual = PasoReserva.SeleccionarPrestador;
    protected void VolverAHorarios() => pasoActual = PasoReserva.SeleccionarTurno;

    protected string ObtenerTextoPaso()
    {
        return pasoActual switch
        {
            PasoReserva.SeleccionarSede => "1. Elegí la sucursal",
            PasoReserva.SeleccionarServicio => "2. ¿Qué te querés hacer?",
            PasoReserva.SeleccionarPrestador => "3. Elegí tu profesional",
            PasoReserva.SeleccionarTurno => "4. Seleccioná día y hora",
            PasoReserva.CompletarDatos => "5. Completá tus datos",
            _ => "Preparando agenda..."
        };
    }

    protected async Task CargarDisponibilidad()
    {
        cargandoHorarios = true;
        slots.Clear();
        prestadoresPorHoraLibre.Clear();
        horaSeleccionada = null;

        try
        {
            // Si no hay prestador, mandamos null
            Guid? idAEnviar = prestadorSeleccionado?.Id;

            var res = await TurnoService.GetDisponibilidad(idAEnviar, sedeSeleccionada!.Id, fechaSeleccionada, servicioSeleccionado!.Id);

            if (res != null)
            {
                foreach (var slot in res.HorariosLibres)
                {
                    slots.Add(slot.Hora); // Para pintar los botones en pantalla
                    prestadoresPorHoraLibre[slot.Hora] = slot.PrestadorId; // Para saber a quién asignarle el turno final
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

    protected async Task ConfirmarReserva()
    {
        errorReserva = string.Empty;
        enviandoReserva = true;

        try
        {
            Guid idPrestadorFinal;
            string nombrePrestadorReserva;

            if (prestadorSeleccionado != null)
            {
                // El usuario eligió específicamente a este profesional
                idPrestadorFinal = prestadorSeleccionado.Id;
                nombrePrestadorReserva = prestadorSeleccionado.Nombre;
            }
            else
            {
                if (prestadoresPorHoraLibre.TryGetValue(horaSeleccionada!, out Guid idPrestadorAsignado))
                {
                    var prestadorAsignado = prestadoresFiltrados.FirstOrDefault(p => p.Id == idPrestadorAsignado);
                    if (prestadorAsignado != null)
                    {
                        idPrestadorFinal = prestadorAsignado.Id;
                        nombrePrestadorReserva = prestadorAsignado.Nombre;
                    }
                    else
                    {
                        var asignado = prestadoresFiltrados.First();
                        idPrestadorFinal = asignado.Id;
                        nombrePrestadorReserva = asignado.Nombre;
                    }
                }
                else
                {
                    var asignado = prestadoresFiltrados.First();
                    idPrestadorFinal = asignado.Id;
                    nombrePrestadorReserva = asignado.Nombre;
                }
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
                var turnoId = resultado.Id;

                // 🎯 NUEVO: Tomamos el teléfono del negocio y lo preparamos para la URL
                var telefonoNegocioEnc = Uri.EscapeDataString(negocio?.Telefono ?? "");

                Nav.NavigateTo($"/reserva-confirmada?fecha={fechaEnc}&hora={horaEnc}&nombre={nombreEnc}&duracion={duracionReal}&direccion={direccionEnc}&TurnoId={turnoId}&TelefonoNegocio={telefonoNegocioEnc}");
            }
            else
            {
                errorReserva = "No pudimos confirmar la reserva. El turno pudo haber sido ocupado recientemente.";
            }
        }
        catch (Exception)
        {
            errorReserva = "El horario seleccionado ya no está disponible. Por favor, elegí otro.";
        }
        finally
        {
            enviandoReserva = false;
        }
    }
}