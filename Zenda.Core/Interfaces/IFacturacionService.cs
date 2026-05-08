using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface IFacturacionService
    {
        Task<FacturacionDto?> GetResumenAsync();
    }
}
