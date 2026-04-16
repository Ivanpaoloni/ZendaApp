using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;
using Zenda.Application.DTOs.Auth;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register-owner")]
    public async Task<IActionResult> RegisterOwner([FromBody] RegisterOwnerDto dto)
    {
        var result = await _authService.RegisterOwnerAsync(dto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string uid, [FromQuery] string t)
    {
        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(t))
            return BadRequest(new AuthResponseDto { Success = false, Message = "Link inválido." });

        // Decodificamos el token acá en el controller porque es un tema de transporte HTTP
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(t));

        var result = await _authService.ConfirmEmailAsync(uid, decodedToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize] // Solo usuarios autenticados (los que entraron por Auto-Login) pueden pedir esto
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation()
    {
        // Extraemos el ID del usuario directamente del token JWT que envió en la cabecera HTTP
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.ResendConfirmationEmailAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [Authorize] // Protegido, solo usuarios con sesión activa
    [HttpGet("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        // Extraemos el ID directo del token viejo que envió en la petición
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.RefreshTokenAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);

        // Por seguridad, solemos devolver 200 OK aunque el mail no exista, 
        // para evitar ataques de enumeración de usuarios.
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // Decodificamos el token acá en el controller porque es un tema de transporte HTTP
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

        // Le pasamos el token limpio al servicio
        var result = await _authService.ResetPasswordAsync(request.Email, decodedToken, request.NewPassword);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
