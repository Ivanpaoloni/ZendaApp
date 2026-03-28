using Zenda.Application.DTOs.Auth;

namespace Zenda.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}