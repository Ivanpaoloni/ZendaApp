using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Zenda.Client.Services;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Client.Pages
{
    public partial class Turnos : ComponentBase
    {
        [SupplyParameterFromQuery(Name = "nuevo")]
        public string? AbrirNuevoTurno { get; set; }

        [Inject] private NegocioClient _negocioService { get; set; } = default!;
        [Inject] private AppState State { get; set; } = default!;
        [Inject] private TurnoClient TurnoService { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        // Estado Core
        protected DateTime fechaFiltro = DateTime.Today;
        protected bool cargando = true;
        protected DateTime ultimaActualizacion = DateTime.Now;

        // Memoria caché local: Traemos +-15 días para evitar requests al cambiar la vista o navegar la semana.
        protected List<TurnoReadDto> turnosDelPeriodo = new();

        // Controladores de UI
        protected string modoVista = "Calendario";
        protected bool mostrarFiltros = false;

        // Variables Filtros
        protected string busquedaCliente = "";
        protected string estadoFiltro = "";
        protected string profesionalFiltro = "";
        protected string sedeFiltro = "";
        protected string servicioFiltro = "";

        // Opciones Dropdown
        protected List<string> listaProfesionalesDropdown = new();
        protected List<string> listaSedesDropdown = new();
        protected List<string> listaServiciosDropdown = new();

        // Estado Modales
        protected bool mostrarModal = false;
        protected TurnoReadDto? turnoAEliminar;
        protected bool procesandoCancelacion = false;
        protected bool mostrarDrawerTurno = false;
        protected bool mostrarModalCobro = false;
        protected TurnoReadDto? turnoACobrar;
        protected bool procesandoCobro = false;
        protected MedioPagoEnum medioPagoSeleccionado = MedioPagoEnum.Efectivo;
        protected string errorCobro = "";
        protected bool exportando = false;

        protected int CantidadFiltrosActivos =>
            (!string.IsNullOrWhiteSpace(busquedaCliente) ? 1 : 0) +
            (!string.IsNullOrEmpty(estadoFiltro) ? 1 : 0) +
            (!string.IsNullOrEmpty(profesionalFiltro) ? 1 : 0) +
            (!string.IsNullOrEmpty(sedeFiltro) ? 1 : 0) +
            (!string.IsNullOrEmpty(servicioFiltro) ? 1 : 0);

        // --- PROPIEDADES COMPUTADAS ---

        // 1. O(N) Filtrado para ZendyScheduler (Calendario)
        protected List<TurnoReadDto> TurnosPlanosFiltrados
        {
            get
            {
                var query = turnosDelPeriodo.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(busquedaCliente))
                    query = query.Where(t => t.ClienteNombre.Contains(busquedaCliente, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(estadoFiltro))
                    query = query.Where(t => t.Estado.ToString() == estadoFiltro);

                if (!string.IsNullOrEmpty(profesionalFiltro))
                    query = query.Where(t => t.PrestadorNombre == profesionalFiltro);

                if (!string.IsNullOrEmpty(sedeFiltro))
                    query = query.Where(t => t.SedeNombre == sedeFiltro);

                if (!string.IsNullOrEmpty(servicioFiltro))
                    query = query.Where(t => t.ServicioNombre == servicioFiltro);

                return query.ToList();
            }
        }

        // 2. O(N) Agrupamiento para Vista Lista Diaria
        protected Dictionary<string, List<TurnoReadDto>> GruposFiltradosDelDia
        {
            get
            {
                // Solo renderiza la fecha exacta que se indica en fechaFiltro
                return TurnosPlanosFiltrados
                    .Where(t => t.FechaHoraInicioUtc.ToLocalTime().Date == fechaFiltro.Date)
                    .GroupBy(t => t.PrestadorNombre)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await CargarTurnosDesdeCero();

            if (AbrirNuevoTurno == "true")
            {
                mostrarDrawerTurno = true;
            }
        }

        protected async Task CargarTurnosDesdeCero()
        {
            cargando = true;
            StateHasChanged();

            try
            {
                // Buscamos 15 días hacia atrás y 15 hacia adelante para soportar la vista semanal fluidamente
                var desde = fechaFiltro.AddDays(-15);
                var hasta = fechaFiltro.AddDays(15);

                turnosDelPeriodo = await TurnoService.GetByRango(desde, hasta, null) ?? new List<TurnoReadDto>();

                // Poblar dropdowns garantizando que no se rompa si hay valores nulos
                listaProfesionalesDropdown = turnosDelPeriodo.Where(t => !string.IsNullOrEmpty(t.PrestadorNombre)).Select(t => t.PrestadorNombre).Distinct().OrderBy(n => n).ToList();
                listaSedesDropdown = turnosDelPeriodo.Where(t => !string.IsNullOrEmpty(t.SedeNombre)).Select(t => t.SedeNombre).Distinct().OrderBy(n => n).ToList();
                listaServiciosDropdown = turnosDelPeriodo.Where(t => !string.IsNullOrEmpty(t.ServicioNombre)).Select(t => t.ServicioNombre).Distinct().OrderBy(n => n).ToList();

                ultimaActualizacion = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando turnos: {ex.Message}");
                turnosDelPeriodo = new();
            }
            finally
            {
                cargando = false;
                StateHasChanged();
            }
        }

        protected async Task CambiarFechaFiltro()
        {
            // Solo disparamos la recarga HTTP si el usuario se mueve muy lejos de la memoria actual.
            await CargarTurnosDesdeCero();
        }

        protected async Task IrAHoy()
        {
            fechaFiltro = DateTime.Today;
            await CargarTurnosDesdeCero();
        }

        protected async Task RefrescarManual()
        {
            await CargarTurnosDesdeCero();
        }

        protected void CambiarModoVista(string modo)
        {
            modoVista = modo;
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

        protected void OnTurnoSeleccionadoDesdeCalendario(TurnoReadDto turno)
        {
            // Disparamos el modal principal (por defecto asumo que prefieres el de Cobro o Detalles)
            MostrarModalCobro(turno);
        }

        // --- MÉTODOS DE MODALES Y ACCIONES ---

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

        protected async Task CambiarEstado(Guid id, EstadoTurnoEnum nuevoEstado)
        {
            try
            {
                var exito = await TurnoService.ActualizarEstado(id, nuevoEstado);
                if (exito)
                {
                    var turnoModificado = turnosDelPeriodo.FirstOrDefault(t => t.Id == id);
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

                var turnoLocal = turnosDelPeriodo.FirstOrDefault(t => t.Id == turnoACobrar.Id);
                if (turnoLocal != null)
                {
                    turnoLocal.Estado = EstadoTurnoEnum.Completado;
                }

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
                var primerDiaMes = new DateTime(fechaFiltro.Year, fechaFiltro.Month, 1);
                var ultimoDiaMes = primerDiaMes.AddMonths(1).AddTicks(-1);

                var stream = await TurnoService.GetExcelStreamAsync(primerDiaMes, ultimoDiaMes);
                if (stream != null)
                {
                    using var streamRef = new DotNetStreamReference(stream);
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
}