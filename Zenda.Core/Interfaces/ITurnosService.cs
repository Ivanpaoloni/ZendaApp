using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface ITurnosService
    {
        Task<IEnumerable<string>> ObtenerHorariosLibresAsync(Guid prestadorId, DateTime fecha);
        Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto);
    }
}
