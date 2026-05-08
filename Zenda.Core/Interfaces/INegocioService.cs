using Zenda.Core.DTOs;
using Zenda.Core.DTOs.Admin;

public interface INegocioService
{
    Task<NegocioReadDto?> GetPublicBySlugAsync(string slug);
    Task<NegocioReadDto?> GetByIdAsync(Guid id);
    Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto);
    Task<NegocioReadDto?> GetPerfilAsync();
    Task<bool> IsSlugAvailableAsync(string slug);
    Task<bool> UpdatePerfilAsync(NegocioUpdateDto dto); 
    Task<bool> UpdateLogoUrlAsync(string logoUrl);
    Task<bool> CambiarAPlanGratuitoAsync(Guid planId);
    Task<bool> ActualizarSuscripcionAdminAsync(Guid negocioId, AdminUpdateNegocioDto dto);
}
