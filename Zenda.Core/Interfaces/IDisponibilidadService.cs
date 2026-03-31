using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IDisponibilidadService
{
    Task<IEnumerable<DisponibilidadReadDto>> GetByPrestadorAsync(Guid prestadorId);
    Task<DisponibilidadReadDto> CreateAsync(DisponibilidadCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UpsertAgendaAsync(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda);
    Task<bool> EliminarBloqueoAsync(Guid id);
    Task<IEnumerable<BloqueoReadDto>> GetBloqueosFuturosAsync(Guid prestadorId);
    Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto);
}