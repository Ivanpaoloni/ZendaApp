using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface ICajaService
    {
        Task<CajaDiariaDto?> GetEstadoCajaHoyAsync(Guid sedeId);
        Task<bool> AbrirCajaAsync(Guid sedeId, decimal montoInicial);
        Task<bool> RegistrarMovimientoManualAsync(NuevoMovimientoDto dto);
    }
}