using Microsoft.AspNetCore.Components;
using Zenda.Client.Services; // Ajusta este namespace según la ubicación de tu AppState y NegocioClient
using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Client.Pages; // Ajusta al namespace de tu proyecto cliente

public partial class Turnos : ComponentBase
{
    [Inject] private NegocioClient _negocioService { get; set; } = default!;
    [Inject] private AppState State { get; set; } = default!;
    [Inject] private TurnoClient TurnoService { get; set; } = default!;

    protected DateTime fechaFiltro = DateTime.Today;
    protected bool cargando = true;
    protected List<TurnoReadDto> turnosDelDia = new();
    protected Dictionary<string, List<TurnoReadDto>> turnosPorProfesional = new();
    protected DateTime ultimaActualizacion = DateTime.Now;

    // Variables de UI para los Filtros
    protected bool mostrarFiltros = false;
    protected string busquedaCliente = "";
    protected string estadoFiltro = "";
    protected string profesionalFiltro = "";
    protected string sedeFiltro = "";
    protected string servicioFiltro = "";

    // Listas para poblar los selects
    protected List<string> listaProfesionalesDropdown = new();
    protected List<string> listaSedesDropdown = new();
    protected List<string> listaServiciosDropdown = new();

    // 🎯 NUEVO: Variables para el Modal
    protected bool mostrarModal = false;
    protected TurnoReadDto? turnoAEliminar;
    protected bool procesandoCancelacion = false;

    // Propiedad calculada que cuenta cuántos filtros están en uso
    protected int CantidadFiltrosActivos =>
        (!string.IsNullOrWhiteSpace(busquedaCliente) ? 1 : 0) +
        (!string.IsNullOrEmpty(estadoFiltro) ? 1 : 0) +
        (!string.IsNullOrEmpty(profesionalFiltro) ? 1 : 0) +
        (!string.IsNullOrEmpty(sedeFiltro) ? 1 : 0) +
        (!string.IsNullOrEmpty(servicioFiltro) ? 1 : 0);

    protected override async Task OnInitializedAsync()
    {
        await CargarTurnos(mantenerFiltros: false);
    }

    // --- MÉTODOS DEL MODAL ---

    protected void MostrarModalCancelacion(TurnoReadDto turno)
    {
        turnoAEliminar = turno;
        mostrarModal = true;
    }

    protected void CerrarModal()
    {
        mostrarModal = false;
        turnoAEliminar = null;
    }

    protected async Task ConfirmarCancelacion()
    {
        if (turnoAEliminar == null) return;

        procesandoCancelacion = true;
        StateHasChanged();

        try
        {
            await CambiarEstado(turnoAEliminar.Id, EstadoTurnoEnum.Cancelado);
            CerrarModal(); // Si tiene éxito, cierra el modal
        }
        finally
        {
            procesandoCancelacion = false;
            StateHasChanged();
        }
    }

    // --- RESTO DE MÉTODOS EXISTENTES ---

    protected void ToggleFiltros() => mostrarFiltros = !mostrarFiltros;

    protected void LimpiarFiltros()
    {
        busquedaCliente = "";
        estadoFiltro = "";
        profesionalFiltro = "";
        sedeFiltro = "";
        servicioFiltro = "";
    }

    protected Dictionary<string, List<TurnoReadDto>> GruposFiltrados
    {
        get
        {
            var filtrados = turnosPorProfesional.AsEnumerable();

            if (!string.IsNullOrEmpty(profesionalFiltro))
            {
                filtrados = filtrados.Where(g => g.Key == profesionalFiltro);
            }

            if (!string.IsNullOrWhiteSpace(busquedaCliente) ||
                !string.IsNullOrEmpty(estadoFiltro) ||
                !string.IsNullOrEmpty(sedeFiltro) ||
                !string.IsNullOrEmpty(servicioFiltro))
            {
                filtrados = filtrados.Select(g => new KeyValuePair<string, List<TurnoReadDto>>(
                    g.Key,
                    g.Value.Where(t =>
                        (string.IsNullOrWhiteSpace(busquedaCliente) || t.ClienteNombre.Contains(busquedaCliente, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrEmpty(estadoFiltro) || t.Estado.ToString() == estadoFiltro) &&
                        (string.IsNullOrEmpty(sedeFiltro) || t.SedeNombre == sedeFiltro) &&
                        (string.IsNullOrEmpty(servicioFiltro) || t.ServicioNombre == servicioFiltro)
                    ).ToList()
                ))
                .Where(g => g.Value.Any());
            }

            return filtrados.ToDictionary(g => g.Key, g => g.Value);
        }
    }

    protected async Task IrAHoy()
    {
        fechaFiltro = DateTime.Today;
        await CargarTurnos(mantenerFiltros: false);
    }

    protected async Task CambiarFecha()
    {
        await CargarTurnos(mantenerFiltros: false);
    }

    protected async Task RefrescarManual()
    {
        await CargarTurnos(mantenerFiltros: true);
    }

    protected async Task CargarTurnos(bool mantenerFiltros)
    {
        cargando = true;
        StateHasChanged();

        try
        {
            turnosDelDia = await TurnoService.GetByFecha(fechaFiltro) ?? new List<TurnoReadDto>();

            listaProfesionalesDropdown = turnosDelDia.Select(t => t.PrestadorNombre).Distinct().OrderBy(n => n).ToList();
            listaSedesDropdown = turnosDelDia.Select(t => t.SedeNombre).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(n => n).ToList();
            listaServiciosDropdown = turnosDelDia.Select(t => t.ServicioNombre).Distinct().OrderBy(n => n).ToList();

            turnosPorProfesional = turnosDelDia
                .GroupBy(t => t.PrestadorNombre)
                .ToDictionary(g => g.Key, g => g.ToList());

            ultimaActualizacion = DateTime.Now;

            if (!mantenerFiltros)
            {
                LimpiarFiltros();
            }
            else
            {
                if (!string.IsNullOrEmpty(profesionalFiltro) && !listaProfesionalesDropdown.Contains(profesionalFiltro)) profesionalFiltro = "";
                if (!string.IsNullOrEmpty(sedeFiltro) && !listaSedesDropdown.Contains(sedeFiltro)) sedeFiltro = "";
                if (!string.IsNullOrEmpty(servicioFiltro) && !listaServiciosDropdown.Contains(servicioFiltro)) servicioFiltro = "";
            }
        }
        catch
        {
            turnosDelDia = new();
            turnosPorProfesional = new();
            listaProfesionalesDropdown = new();
            listaSedesDropdown = new();
            listaServiciosDropdown = new();
        }
        finally
        {
            cargando = false;
        }
    }

    protected async Task CambiarEstado(Guid id, EstadoTurnoEnum nuevoEstado)
    {
        try
        {
            var exito = await TurnoService.ActualizarEstado(id, nuevoEstado);
            if (exito)
            {
                var turnoModificado = turnosDelDia.FirstOrDefault(t => t.Id == id);
                if (turnoModificado != null)
                {
                    turnoModificado.Estado = nuevoEstado;
                    // No hace falta llamar a StateHasChanged acá si viene de ConfirmarCancelacion, 
                    // porque el bloque finally de ese método ya lo hace, pero dejarlo no hace daño.
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cambiar estado: {ex.Message}");
            // Sería ideal mostrar un toast de error aquí si falla la API
        }
    }

    protected string GenerarWhatsAppUrl(string telefono, string nombre, DateTime inicioLocal)
    {
        var nombreNegocio = State.CurrentNegocio?.Nombre ?? "nuestro local";
        var mensaje = $"¡Hola {nombre}! Te escribo de {nombreNegocio} para recordarte tu turno de hoy a las {inicioLocal:HH:mm} hs.";
        var telefonoLimpio = new string(telefono.Where(char.IsDigit).ToArray());
        return $"https://wa.me/{telefonoLimpio}?text={Uri.EscapeDataString(mensaje)}";
    }
}