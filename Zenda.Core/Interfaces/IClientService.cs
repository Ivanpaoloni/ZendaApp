using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IClienteService
{
    Task<IEnumerable<ClienteReadDto>> GetAllAsync();
    Task<IEnumerable<TurnoReadDto>> GetHistorialTurnosAsync(Guid clienteId);
}