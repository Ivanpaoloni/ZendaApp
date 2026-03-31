using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface IBloqueoService
    {
        Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto);
    }
}
