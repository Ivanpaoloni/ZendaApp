using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface IUsuarioService
    {
        Task<UsuarioPerfilDto?> GetPerfilAsync(string userId);
        Task<bool> UpdatePerfilAsync(string userId, UsuarioUpdateDto dto);
    }
}
