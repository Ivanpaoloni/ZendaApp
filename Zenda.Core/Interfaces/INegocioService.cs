using Zenda.Core.DTOs;

public interface INegocioService
{
    Task<NegocioReadDto?> GetPublicBySlugAsync(string slug);
    Task<NegocioReadDto?> GetByIdAsync(Guid id);
    Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto);
    Task<NegocioReadDto?> GetPerfilAsync();
    Task<bool> IsSlugAvailableAsync(string slug);
    Task<bool> UpdatePerfilAsync(NegocioUpdateDto dto); 
    Task<bool> UpdateLogoUrlAsync(string logoUrl);
}