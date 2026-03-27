using Zenda.Core.DTOs;

public interface INegocioService
{
    Task<NegocioReadDto?> GetByIdAsync(Guid id);
    Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto);
}