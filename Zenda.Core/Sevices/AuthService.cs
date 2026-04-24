using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Zenda.Application.DTOs.Auth;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IZendaDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly INegocioService _negocioService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IZendaDbContext context,
        IConfiguration config,
        IEmailService emailService,
        INegocioService negocioService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _config = config;
        _emailService = emailService;
        _negocioService = negocioService;
    }

    public async Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto)
    {
        // 1. Verificamos si el usuario o el slug ya existen
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            return new AuthResponseDto { Success = false, Message = "El email ya está registrado." };

        if (await _context.Negocios.AnyAsync(n => n.Slug == dto.SlugNegocio))
            return new AuthResponseDto { Success = false, Message = "El slug del negocio ya está en uso." };

        // 2. Iniciamos una transacción para que todo sea atómico
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 3. Crear el Negocio
            var nuevoNegocio = new Core.DTOs.NegocioCreateDto
            {
                Nombre = dto.NombreNegocio,
                Slug = dto.SlugNegocio,
                RubroId = dto.RubroId,
                PlanSuscripcionId = Guid.Parse("11111111-1111-1111-1111-111111111111") // Plan Gratis por defecto
            };
            var creadoNegocio = await _negocioService.CreateAsync(nuevoNegocio);
            
            await _context.SaveChangesAsync();

            // 4. Crear el Usuario (Owner)
            var newUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                NegocioId = creadoNegocio.Id // ¡Acá atamos el usuario al tenant!
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);
            
            if (result.Succeeded)
            {
                try
                {
                    // Generar token de Identity y codificarlo para URL
                    var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

                    var frontUrl = _config["FrontendUrl"]; // ej: https://localhost:5001 configurado en appsettings.json
                    var confirmLink = $"{frontUrl}/confirmar-email?uid={newUser.Id}&t={encodedToken}";

                    // Asegurate de que tu IEmailService reciba este nuevo parámetro 'confirmLink'
                    await _emailService.EnviarBienvenidaRegistroAsync(newUser.Email, newUser.Nombre, nuevoNegocio.Nombre, confirmLink);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando email de bienvenida: {ex.Message}");
                }
            }

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return new AuthResponseDto { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };
            }

            // 5. Crear el rol "Owner" si no existe y asignarlo
            if (!await _roleManager.RoleExistsAsync("Owner"))
                await _roleManager.CreateAsync(new IdentityRole("Owner"));

            await _userManager.AddToRoleAsync(newUser, "Owner");

            // Confirmamos la transacción
            await transaction.CommitAsync();

            // GENERAR TOKEN AUTOMÁTICAMENTE
            var token = await GenerateJwtToken(newUser);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Dueño y negocio creados con éxito.",
                Token = token // <--- Agregamos el token aquí
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new AuthResponseDto { Success = false, Message = $"Error interno: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            return new AuthResponseDto { Success = false, Message = "Credenciales inválidas." };
        }

        var token = await GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login exitoso.",
            Token = token
        };
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        // 1. Definimos los Claims (los datos que viajan dentro del token)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.GivenName, user.Nombre),
            new Claim("NegocioId", user.NegocioId.ToString() ?? string.Empty),
            new Claim("email_verified", user.EmailConfirmed.ToString().ToLower())
        };

        // Agregar los roles del usuario a los claims
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // 2. Firmar el token
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "Usuario no encontrado." };

        // Como vuelve a generar el token, ahora leerá EmailConfirmed = true de la BD
        var token = await GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Token = token
        };
    }

    public async Task<AuthResponseDto> ConfirmEmailAsync(string userId, string decodedToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "Usuario no encontrado." };

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
            return new AuthResponseDto { Success = false, Message = "El link expiró o es inválido." };

        // Generar un nuevo token ahora que EmailConfirmed es true
        var nuevoJwt = await GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "¡Email confirmado con éxito!",
            Token = nuevoJwt
        };
    }

    public async Task<AuthResponseDto> ResendConfirmationEmailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || user.EmailConfirmed)
            return new AuthResponseDto { Success = false, Message = "No requiere confirmación o el usuario no existe." };

        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

        var frontUrl = _config["FrontendUrl"];
        var confirmLink = $"{frontUrl}/confirmar-email?uid={user.Id}&t={encodedToken}";

        // Acá usás tu servicio de email (asumiendo que tenés un método para esto)
        await _emailService.EnviarEmailConfirmacionAsync(user.Email, user.Nombre, confirmLink);

        return new AuthResponseDto { Success = true, Message = "Email reenviado correctamente." };
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Si el usuario no existe o no tiene el email confirmado, no hacemos nada,
        // pero devolvemos éxito para no dar pistas a posibles atacantes.
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            return new AuthResponseDto { Success = true, Message = "Si el correo existe, recibirás un enlace." };
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Codificamos el token para la URL
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // CORRECCIÓN: Usamos _config en lugar de _configuration
        var frontendUrl = _config["FrontendUrl"] ?? "https://app.zendy.com.ar";
        var resetLink = $"{frontendUrl}/restablecer-contrasena?email={request.Email}&token={encodedToken}";

        await _emailService.EnviarRecuperacionClaveAsync(request.Email, resetLink);

        return new AuthResponseDto { Success = true, Message = "Si el correo existe, recibirás un enlace." };
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(string email, string decodedToken, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new AuthResponseDto { Success = false, Message = "Error al restablecer la contraseña." };
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

        if (result.Succeeded)
        {
            return new AuthResponseDto { Success = true, Message = "Contraseña restablecida correctamente." };
        }

        return new AuthResponseDto { Success = false, Message = "El enlace no es válido o ha expirado." };
    }
}