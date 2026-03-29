using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IServicioService
{
    Task<IEnumerable<CategoriaServicioReadDto>> GetCatalogoAsync();
    Task<CategoriaServicioReadDto> CreateCategoriaAsync(CategoriaServicioCreateDto dto);
    Task<ServicioReadDto> CreateServicioAsync(ServicioCreateDto dto);
    // Agregamos este que nos va a servir para la reserva pública más adelante
    Task<IEnumerable<ServicioReadDto>> GetServiciosPublicosBySedeAsync(Guid sedeId);
    Task<IEnumerable<ServicioPublicoDto>> GetServiciosPublicosPorSedeAsync(Guid sedeId);
}