using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class MagicLinkService : IMagicLinkService
{
    private readonly IConfiguration _config;

    public MagicLinkService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerarTokenIntegracion(int prestadorId, int expiracionHoras = 24)
    {
        // En producción, asegúrate de tener "Jwt:Key", "Jwt:Issuer" y "Jwt:Audience" en appsettings.json
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "ClaveSuperSecretaDeDesarrolloZendy123!"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim("PrestadorId", prestadorId.ToString()),
            new Claim("Tipo", "GoogleCalendarSync")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiracionHoras),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ExtraerPrestadorId(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == "PrestadorId");

            return claim != null ? int.Parse(claim.Value) : null;
        }
        catch
        {
            return null; // Token inválido o manipulado
        }
    }
}