using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IDisponibilidadService
{
    Task<IEnumerable<DisponibilidadReadDto>> GetByPrestadorAsync(Guid prestadorId);
    Task<DisponibilidadReadDto> CreateAsync(DisponibilidadCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UpsertAgendaAsync(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda);
}