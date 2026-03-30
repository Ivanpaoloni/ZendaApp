using Microsoft.AspNetCore.Identity;
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

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IZendaDbContext context,
        IConfiguration config)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _config = config;
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
            var nuevoNegocio = new Negocio
            {
                Nombre = dto.NombreNegocio,
                Slug = dto.SlugNegocio
            };

            _context.Negocios.Add(nuevoNegocio);
            await _context.SaveChangesAsync();

            // 4. Crear el Usuario (Owner)
            var newUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                NegocioId = nuevoNegocio.Id // ¡Acá atamos el usuario al tenant!
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);

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

            return new AuthResponseDto { Success = true, Message = "Dueño y negocio creados con éxito." };
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
            // ¡ESTE ES EL CLAIM CLAVE PARA TU TENANTSERVICE!
            new Claim("NegocioId", user.NegocioId.ToString() ?? string.Empty)
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
}