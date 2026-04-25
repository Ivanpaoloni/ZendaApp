using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Zenda.Client.Services; // Ajusta este namespace según la ubicación de tu AppState y NegocioClient
using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Client.Pages; // Ajusta al namespace de tu proyecto cliente

public partial class Turnos : ComponentBase
{
    [Inject] private NegocioClient _negocioService { get; set; } = default!;
    [Inject] private AppState State { get; set; } = default!;
    [Inject] private TurnoClient TurnoService { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

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

    // modales
    protected bool mostrarModal = false;
    protected TurnoReadDto? turnoAEliminar;
    protected bool procesandoCancelacion = false;

    protected bool mostrarModalCobro = false;
    protected TurnoReadDto? turnoACobrar;
    protected bool procesandoCobro = false;
    protected MedioPagoEnum medioPagoSeleccionado = MedioPagoEnum.Efectivo;
    protected string errorCobro = "";
    private bool exportando = false;
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
            CerrarModal();
        }
        finally
        {
            procesandoCancelacion = false;
            StateHasChanged();
        }
    }

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
    protected void MostrarModalCobro(TurnoReadDto turno)
    {
        errorCobro = "";
        medioPagoSeleccionado = MedioPagoEnum.Efectivo;
        turnoACobrar = turno;
        mostrarModalCobro = true;
    }

    protected void CerrarModalCobro()
    {
        mostrarModalCobro = false;
        turnoACobrar = null;
    }

    protected async Task ConfirmarCobro()
    {
        if (turnoACobrar == null) return;

        procesandoCobro = true;
        errorCobro = "";
        StateHasChanged();

        try
        {
            await TurnoService.CobrarTurno(turnoACobrar.Id, medioPagoSeleccionado);

            turnoACobrar.Estado = EstadoTurnoEnum.Completado;
            CerrarModalCobro();
        }
        catch (Exception ex)
        {
            errorCobro = ex.Message;
        }
        finally
        {
            procesandoCobro = false;
            StateHasChanged();
        }
    }

    private async Task ExportarExcelMes()
    {
        exportando = true;
        StateHasChanged();

        try
        {
            // Calculamos el mes en base a la fecha que el usuario está mirando en pantalla
            var primerDiaMes = new DateTime(fechaFiltro.Year, fechaFiltro.Month, 1);

            // Calculamos el último día de ese mismo mes a las 23:59:59 para no perder turnos de última hora
            var ultimoDiaMes = primerDiaMes.AddMonths(1).AddTicks(-1);

            var stream = await TurnoService.GetExcelStreamAsync(primerDiaMes, ultimoDiaMes);
            if (stream != null)
            {
                using var streamRef = new DotNetStreamReference(stream);

                // Usamos el formato Zendy_YYYYMMDD_HHmm que me pediste antes
                await JS.InvokeVoidAsync("downloadFileFromStream", $"Turnos_Zendy_{DateTime.Now:yyyyMMdd_HHmm}.xlsx", streamRef);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exportando turnos: {ex.Message}");
        }
        finally
        {
            exportando = false;
        }
    }
}