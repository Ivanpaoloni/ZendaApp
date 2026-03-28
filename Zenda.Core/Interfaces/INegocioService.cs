using Zenda.Core.DTOs;

public interface INegocioService
{
    Task<NegocioReadDto?> GetPublicBySlugAsync(string slug);
    Task<NegocioReadDto?> GetByIdAsync(Guid id);
    Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto);
    Task<NegocioReadDto?> GetPerfilAsync();
}