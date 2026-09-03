using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Zenda.Core.DTOs;

namespace Zenda.Client.Components
{
    public partial class ZendyScheduler : ComponentBase
    {
        [Parameter] public List<TurnoReadDto> Turnos { get; set; } = new();
        [Parameter] public string TimeZoneId { get; set; } = "America/Argentina/Buenos_Aires";
        [Parameter] public DateTime FechaBase { get; set; } = DateTime.Today;
        [Parameter] public EventCallback<DateTime> FechaBaseChanged { get; set; }
        [Parameter] public EventCallback<TurnoReadDto> OnTurnoSeleccionado { get; set; }

        // Nuevo evento para la creación de turnos en slots vacíos
        [Parameter] public EventCallback<(DateTime Fecha, TimeSpan Hora)> OnSlotVacioSeleccionado { get; set; }

        [Inject] public IJSRuntime JS { get; set; } = default!;

        // Por defecto en "Diaria" para evitar scroll horizontal roto en Mobile
        private bool VistaSemanal { get; set; } = false;

        // FIX: antes el auto-scroll (horizontal a "hoy" + vertical a las 8am) solo se
        // disparaba en el primer render del componente. Cambiar de Diaria a Semanal, o
        // navegar con las flechas de período, no volvía a dispararlo — por eso la vertical
        // "funcionaba" (coincidía con el primer render al abrir Calendario) pero la
        // horizontal nunca se veía (en Diaria no hay nada que scrollear; recién hace falta
        // al pasar a Semanal, y ahí no se volvía a llamar).
        private bool _debeScrollear = true;
        private DateTime? _fechaBaseAnterior;

        protected override void OnParametersSet()
        {
            // FIX: si el padre cambia FechaBase desde afuera (ej. el botón "Hoy" del
            // toolbar en Turnos.razor) también queremos re-centrar, no solo cuando se
            // navega con las flechas internas de este componente.
            if (_fechaBaseAnterior.HasValue && _fechaBaseAnterior.Value.Date != FechaBase.Date)
            {
                _debeScrollear = true;
            }
            _fechaBaseAnterior = FechaBase;
        }

        // FIX: fallback si llega null/vacío (evita excepción de FindSystemTimeZoneById)
        private TimeZoneInfo ZonaHorariaLocal =>
            TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(TimeZoneId) ? "America/Argentina/Buenos_Aires" : TimeZoneId);

        private IEnumerable<TurnoReadDto> TurnosVisibles
        {
            get
            {
                if (Turnos == null || !Turnos.Any()) return Enumerable.Empty<TurnoReadDto>();
                var inicioPeriodo = ObtenerInicioPeriodo();
                var finPeriodo = VistaSemanal ? inicioPeriodo.AddDays(7) : inicioPeriodo.AddDays(1);

                return Turnos.Where(t =>
                {
                    var localStart = TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraInicioUtc, ZonaHorariaLocal);
                    return localStart >= inicioPeriodo && localStart < finPeriodo;
                });
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender || _debeScrollear)
            {
                _debeScrollear = false;
                await EjecutarScrollAutomatico();
            }
        }

        // Centra horizontalmente la columna de hoy (si está visible) y verticalmente
        // en las 8am. Se llama en el primer render Y cada vez que cambia la vista o
        // el período mostrado.
        private async Task EjecutarScrollAutomatico()
        {
            try
            {
                // Pequeño delay para asegurar que el DOM ya renderizó la línea roja
                await Task.Delay(80);
                await JS.InvokeVoidAsync("eval", @"
                    (function() {
                        var contenedor = document.getElementById('calendar-scroll-container');
                        if (!contenedor) return;

                        // 1. Scroll vertical: centrar la línea de hora actual.
                        //    Si no existe (día sin línea roja, ej. semana futura),
                        //    hacer scroll a las 8am como fallback (480px = 8 * 60px/h).
                        var lineaRoja = document.getElementById('linea-hora-actual');
                        if (lineaRoja) {
                            var offsetTop = lineaRoja.offsetTop;
                            var mitadContenedor = contenedor.clientHeight / 2;
                            contenedor.scrollTop = offsetTop - mitadContenedor;
                        } else {
                            contenedor.scrollTop = 480; // 8am fallback
                        }

                        // 2. Scroll horizontal: centrar la columna de hoy (solo semanal).
                        var colHoy = document.getElementById('columna-hoy');
                        if (colHoy) {
                            colHoy.scrollIntoView({ inline: 'center', block: 'nearest' });
                        }
                    })();
                ");
            }
            catch
            {
                // Ignorar durante prerender o si el DOM aún no está listo
            }
        }

        private void CambiarVista(bool esSemanal)
        {
            VistaSemanal = esSemanal;
            _debeScrollear = true;
        }

        private async Task Retroceder()
        {
            FechaBase = VistaSemanal ? FechaBase.AddDays(-7) : FechaBase.AddDays(-1);
            _debeScrollear = true;
            await FechaBaseChanged.InvokeAsync(FechaBase);
        }

        private async Task Avanzar()
        {
            FechaBase = VistaSemanal ? FechaBase.AddDays(7) : FechaBase.AddDays(1);
            _debeScrollear = true;
            await FechaBaseChanged.InvokeAsync(FechaBase);
        }

        private DateTime ObtenerInicioPeriodo()
        {
            if (!VistaSemanal) return FechaBase.Date;
            int diff = (7 + (FechaBase.DayOfWeek - DayOfWeek.Monday)) % 7;
            return FechaBase.AddDays(-1 * diff).Date;
        }

        private string ObtenerTextoFecha()
        {
            var culture = new CultureInfo("es-AR");
            if (!VistaSemanal) return FechaBase.ToString("dddd, d 'de' MMMM", culture).ToUpper();
            var inicioSemana = ObtenerInicioPeriodo();
            var finSemana = inicioSemana.AddDays(6);
            return $"{inicioSemana.Day} al {finSemana.ToString("d 'de' MMMM", culture)}".ToUpper();
        }

        private int CalcularColumna(DateTime fechaLocal)
        {
            if (!VistaSemanal) return 2;
            int offset = fechaLocal.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fechaLocal.DayOfWeek;
            return offset + 1; // +1 porque la columna 1 es la etiqueta de horas
        }

        // ================================================================
        // FIX PRINCIPAL: para elementos con position:absolute, CSS Grid
        // resuelve un "grid-column-end" implícito (auto) extendiéndolo
        // hasta el borde derecho del contenedor completo, NO hasta el
        // borde de esa columna. Por eso las cards se estiraban desde el
        // día actual hasta el final de la semana.
        // Este helper arma SIEMPRE "inicio / fin" explícito para que el
        // área de grid (y por lo tanto el contenedor de posicionamiento
        // absoluto) quede acotada a exactamente 1 columna de ancho.
        // ================================================================
        private static string GridColumnaAcotada(int columnaInicio) => $"{columnaInicio} / {columnaInicio + 1}";

        private bool MuestraElDiaDeHoy()
        {
            var hoy = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHorariaLocal).Date;
            var inicio = ObtenerInicioPeriodo();
            var fin = VistaSemanal ? inicio.AddDays(7) : inicio.AddDays(1);
            return hoy >= inicio && hoy < fin;
        }

        private async Task IniciarNuevoTurno(DateTime fechaColumna, int hora, int minutos)
        {
            if (OnSlotVacioSeleccionado.HasDelegate)
            {
                var timeSpan = new TimeSpan(hora, minutos, 0);
                await OnSlotVacioSeleccionado.InvokeAsync((fechaColumna.Date, timeSpan));
            }
        }

        // Algoritmo para agrupar y dividir anchos de turnos solapados
        private List<TurnoUIModel> ProcesarTurnosVisuales(IEnumerable<TurnoReadDto> turnos)
        {
            var uiModels = turnos.Select(t => {
                var localStart = TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraInicioUtc, ZonaHorariaLocal);
                var localEnd = TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraFinUtc, ZonaHorariaLocal);
                var alto = Math.Max((int)(localEnd - localStart).TotalMinutes, 12);
                return new TurnoUIModel
                {
                    Data = t,
                    TopPx = (localStart.Hour * 60) + localStart.Minute,
                    HeightPx = alto, // alto mínimo visible
                    ColumnaGrid = CalcularColumna(localStart),
                    // FIX: turnos cortos no entran 2-3 líneas apiladas sin recortarse.
                    // Por debajo de este umbral usamos layout de una sola línea (ver .razor/.css)
                    EsCompacto = alto < 40
                };
            }).OrderBy(t => t.TopPx).ToList();

            var turnosPorColumna = uiModels.GroupBy(t => t.ColumnaGrid);

            foreach (var grupo in turnosPorColumna)
            {
                List<List<TurnoUIModel>> columnasSolapamiento = new();
                foreach (var turno in grupo)
                {
                    bool colocado = false;
                    foreach (var col in columnasSolapamiento)
                    {
                        if (!col.Any(t => t.TopPx < turno.TopPx + turno.HeightPx && t.TopPx + t.HeightPx > turno.TopPx))
                        {
                            col.Add(turno);
                            colocado = true;
                            break;
                        }
                    }
                    if (!colocado) columnasSolapamiento.Add(new List<TurnoUIModel> { turno });
                }

                int numCols = columnasSolapamiento.Count;
                for (int i = 0; i < numCols; i++)
                {
                    foreach (var turno in columnasSolapamiento[i])
                    {
                        turno.WidthPorcentaje = 100.0 / numCols;
                        turno.LeftPorcentaje = i * (100.0 / numCols);
                    }
                }
            }
            return uiModels;
        }
    }

    public class TurnoUIModel
    {
        public TurnoReadDto Data { get; set; }
        public int TopPx { get; set; }
        public int HeightPx { get; set; }
        public double WidthPorcentaje { get; set; }
        public double LeftPorcentaje { get; set; }
        public int ColumnaGrid { get; set; }
        public bool EsCompacto { get; set; }
    }
}
