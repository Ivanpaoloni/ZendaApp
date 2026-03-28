using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface ITurnosService
{
    Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto);
    Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId);
    Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid prestadorId, DateTime fecha); 
    Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha);
}