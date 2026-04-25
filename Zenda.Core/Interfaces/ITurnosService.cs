using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Core.Interfaces;

public interface ITurnosService
{
    Task<TurnoReadDto> GetByIdAsync(Guid id);
    Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto);
    Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId); 
    Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid? prestadorId, Guid sedeId, DateTime fecha, Guid servicioId);
    Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha);
    Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado);
    Task<bool> CancelarPorClienteAsync(Guid turnoId);
    Task<TurnoReadDto> GetResumenPublicoAsync(Guid turnoId);
    Task<DashboardResumenDto> GetDashboardResumenAsync();
    Task<bool> FinalizarYCobrarTurnoAsync(Guid turnoId, MedioPagoEnum medioPago); 
    Task<byte[]> GenerarReporteExcelAsync(DateTime desde, DateTime hasta);
}