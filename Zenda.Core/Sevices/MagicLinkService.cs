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

    // 1. Cambiamos de int a Guid
    public string GenerarTokenIntegracion(Guid prestadorId, int expiracionHoras = 24)
    {
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

    // 2. Renombramos para que coincida con el Controller y pasamos a Guid?
    public Guid? ValidarTokenIntegracion(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "ClaveSuperSecretaDeDesarrolloZendy123!"));

            // ⚠️ AQUÍ ESTÁ LA MAGIA DE SEGURIDAD: ValidateToken en lugar de ReadJwtToken
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,

                // Si tienes Issuer y Audience en tu appsettings, pon esto en true
                ValidateIssuer = false,
                ValidateAudience = false,

                // Esto es CRUCIAL: Verifica automáticamente si el token expiró
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Para que la expiración sea exacta

            }, out SecurityToken validatedToken);

            // Si llega a esta línea, el token es 100% legítimo, no fue alterado y no expiró
            var jwtToken = (JwtSecurityToken)validatedToken;
            var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == "PrestadorId");

            // Parseamos a Guid
            return claim != null ? Guid.Parse(claim.Value) : null;
        }
        catch (SecurityTokenExpiredException)
        {
            // El token venció (pasaron las 24 horas)
            return null;
        }
        catch
        {
            // El token fue alterado, la firma no coincide o está corrupto
            return null;
        }
    }
}