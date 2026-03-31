using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    // 🎯 Inyectamos el servicio en lugar del UserManager
    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UsuarioPerfilDto>> GetMiPerfil()
    {
        // El controlador extrae la identidad del Token
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // El servicio hace el trabajo
        var perfil = await _usuarioService.GetPerfilAsync(userId);

        if (perfil == null) return NotFound();

        return Ok(perfil);
    }

    [HttpPut("me")]
    public async Task<ActionResult> UpdateMiPerfil(UsuarioUpdateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var actualizado = await _usuarioService.UpdatePerfilAsync(userId, dto);

        if (actualizado)
            return NoContent();

        return BadRequest(new { message = "Error al actualizar el perfil" });
    }

}