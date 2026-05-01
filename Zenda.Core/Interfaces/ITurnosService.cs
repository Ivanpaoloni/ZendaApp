using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Core.Interfaces;

public interface ITurnosService
{
    //get
    Task<TurnoReadDto> GetByIdAsync(Guid id);
    Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId); 
    Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid? prestadorId, Guid sedeId, DateTime fecha, Guid servicioId);
    Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha);
    Task<TurnoReadDto> GetResumenPublicoAsync(Guid turnoId);
    Task<DashboardResumenDto> GetDashboardResumenAsync();

    //create/update
    Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto);
    Task<TurnoReadDto> CrearTurnoAdminAsync(TurnoAdminCreateDto dto);
    Task<byte[]> GenerarReporteExcelAsync(DateTime desde, DateTime hasta);
    Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado);
    Task<bool> CancelarPorClienteAsync(Guid turnoId);
    Task<bool> FinalizarYCobrarTurnoAsync(Guid turnoId, MedioPagoEnum medioPago); 
}