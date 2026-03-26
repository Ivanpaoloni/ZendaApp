
using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface ISedeService
    {
        Task<IEnumerable<SedeReadDto>> GetAllAsync();
    }
}
