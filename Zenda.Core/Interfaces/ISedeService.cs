
using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface ISedeService
    {
        Task<SedeReadDto> CreateAsync(SedeCreateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<SedeReadDto>> GetAllAsync();
        Task<IEnumerable<SedeReadDto>> GetPublicByNegocioIdAsync(Guid negocioId);
    }
}
