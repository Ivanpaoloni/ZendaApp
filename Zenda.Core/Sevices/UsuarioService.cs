using Microsoft.AspNetCore.Identity;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;
namespace Zenda.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly UserManager<ApplicationUser> _userManager;
    public UsuarioService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UsuarioPerfilDto?> GetPerfilAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null) return null;

        return new UsuarioPerfilDto
        {
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email ?? string.Empty,
            Telefono = user.PhoneNumber
        };
    }

    public async Task<bool> UpdatePerfilAsync(string userId, UsuarioUpdateDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null) return false;

        // Actualizamos los datos
        user.Nombre = dto.Nombre;
        user.Apellido = dto.Apellido;
        user.PhoneNumber = dto.Telefono;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }
}