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
        [Inject] private SedeClient SedeService { get; set; } = default!; // INYECTAMOS SEDE CLIENT
        [Inject] public IJSRuntime JS { get; set; } = default!;

        // Estado Core
        protected DateTime fechaFiltro = DateTime.Today;
        protected bool cargando = true;
        protected DateTime ultimaActualizacion = DateTime.Now;

        // Memoria caché local
        protected List<TurnoReadDto> turnosDelPeriodo = new();
        protected List<SedeReadDto> sedesCompletas = new(); // ALMACENA LAS SEDES CON SU TIMEZONE

        // Controladores de UI
        protected string modoVista = "Lista";
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
        protected bool mostrarModalDetalle = false;
        protected TurnoReadDto? turnoSeleccionado;

        protected int CantidadFiltrosActivos =>
            (!string.IsNullOrWhiteSpace(busquedaCliente) ? 1 : 0) +
            (!string.IsNullOrEmpty(estadoFiltro) ? 1 : 0) +
            (!string.IsNullOrEmpty(profesionalFiltro) ? 1 : 0) +
            (!string.IsNullOrEmpty(sedeFiltro) ? 1 : 0) +
            (!string.IsNullOrEmpty(servicioFiltro) ? 1 : 0);

        // --- PROPIEDADES COMPUTADAS ---

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

        protected List<TurnoReadDto> TurnosDelDiaSeleccionado
        {
            get
            {
                return TurnosPlanosFiltrados
                    .Where(t => t.FechaHoraInicioUtc.ToLocalTime().Date == fechaFiltro.Date)
                    .OrderBy(t => t.FechaHoraInicioUtc)
                    .ToList();
            }
        }

        // --- MÉTODO PARA RESOLVER EL TIMEZONE DINÁMICO ---
        protected string ObtenerTimeZoneActual()
        {
            if (!string.IsNullOrEmpty(sedeFiltro) && sedesCompletas.Any())
            {
                var sedeSeleccionada = sedesCompletas.FirstOrDefault(s => s.Nombre == sedeFiltro);
                if (sedeSeleccionada != null && !string.IsNullOrEmpty(sedeSeleccionada.ZonaHorariaId))
                {
                    return sedeSeleccionada.ZonaHorariaId;
                }
            }
            // Fallback a Argentina si no hay sede seleccionada o no tiene TZ
            return "America/Argentina/Buenos_Aires";
        }

        protected override async Task OnInitializedAsync()
        {
            // Cargamos la lista completa de sedes en memoria para tener sus TimeZones
            try
            {
                if (State.CurrentNegocio != null)
                {
                    // Asume un método similar en tu SedeService para traer las sedes
                    sedesCompletas = await SedeService.GetAll() ?? new();
                }
            }
            catch { /* Log o ignorar, usará fallback */ }

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
                var desde = fechaFiltro.AddDays(-15);
                var hasta = fechaFiltro.AddDays(15);

                turnosDelPeriodo = await TurnoService.GetByRango(desde, hasta, null) ?? new List<TurnoReadDto>();

                listaProfesionalesDropdown = turnosDelPeriodo.Where(t => !string.IsNullOrEmpty(t.PrestadorNombre)).Select(t => t.PrestadorNombre).Distinct().OrderBy(n => n).ToList();
                listaSedesDropdown = turnosDelPeriodo.Where(t => !string.IsNullOrEmpty(t.SedeNombre)).Select(t => t.SedeNombre).Distinct().OrderBy(n => n).ToList();
                listaServiciosDropdown = turnosDelPeriodo.Where(t => !string.IsNullOrEmpty(t.ServicioNombre)).Select(t => t.ServicioNombre).Distinct().OrderBy(n => n).ToList();

                if (listaProfesionalesDropdown.Any() && !listaProfesionalesDropdown.Contains(profesionalFiltro))
                {
                    profesionalFiltro = listaProfesionalesDropdown.First();
                }

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
            turnoSeleccionado = turno;
            mostrarModalDetalle = true;
        }

        protected void CerrarModalDetalle()
        {
            mostrarModalDetalle = false;
            turnoSeleccionado = null;
        }

        protected void PrepararCobroDesdeDetalle()
        {
            if (turnoSeleccionado == null) return;
            var turnoTemp = turnoSeleccionado;
            CerrarModalDetalle();
            MostrarModalCobro(turnoTemp);
        }

        protected void PrepararCancelacionDesdeDetalle()
        {
            if (turnoSeleccionado == null) return;
            var turnoTemp = turnoSeleccionado;
            CerrarModalDetalle();
            MostrarModalCancelacion(turnoTemp);
        }

        protected async Task AvanzarDia()
        {
            fechaFiltro = fechaFiltro.AddDays(1);
            await CambiarFechaFiltro();
        }

        protected async Task RetrocederDia()
        {
            fechaFiltro = fechaFiltro.AddDays(-1);
            await CambiarFechaFiltro();
        }

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