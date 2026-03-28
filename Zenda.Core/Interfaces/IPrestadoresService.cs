using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IPrestadoresService
{
    Task<IEnumerable<PrestadorReadDto>> GetAllAsync();
    Task<PrestadorReadDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<PrestadorReadDto>> GetBySedeAsync(Guid sedeId);
    Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto);
    Task<bool> UpdateAsync(Guid id, PrestadorUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}