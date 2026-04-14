using Zenda.Application.DTOs.Auth;

namespace Zenda.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto); 
        Task<AuthResponseDto> ConfirmEmailAsync(string userId, string decodedToken);
        Task<AuthResponseDto> ResendConfirmationEmailAsync(string userId);
        Task<AuthResponseDto> RefreshTokenAsync(string userId);
    }
}