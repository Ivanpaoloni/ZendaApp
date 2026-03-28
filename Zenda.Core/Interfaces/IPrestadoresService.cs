using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IPrestadoresService
{
    Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<PrestadorReadDto>> GetAllAsync();
    Task<PrestadorReadDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<PrestadorReadDto>> GetPublicBySedeIdAsync(Guid sedeId);
    Task<bool> UpdateAsync(Guid id, PrestadorUpdateDto dto);
}