using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Core.Interfaces;

public interface ITurnosService
{
    Task<TurnoReadDto> GetByIdAsync(Guid id);
    Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto);
    Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId);
    Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid prestadorId, DateTime fecha, Guid servicioId); 
    Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha);
    Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado);
}