using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace Zenda.Client.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider   
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken"); // O donde guardes tu token

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // 1. Parseamos los claims del token
        var claims = ParseClaimsFromJwt(token);

        // 2. 🎯 NUEVO: Buscamos la fecha de expiración ("exp")
        var expiracionClaim = claims.FirstOrDefault(c => c.Type == "exp");

        if (expiracionClaim != null)
        {
            // Convertimos el número "Unix" a fecha real
            var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiracionClaim.Value));

            // Si la fecha de expiración ya pasó...
            if (expTime <= DateTimeOffset.UtcNow)
            {
                // Borramos el token muerto y lo mandamos a loguearse
                await _localStorage.RemoveItemAsync("authToken");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        // Si todo está bien, lo dejamos pasar
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    // Método para avisar a la app que alguien se logueó
    public void NotifyUserAuthentication(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    // Método para avisar que alguien cerró sesión
    public void NotifyUserLogout()
    {
        // Creamos un usuario "anónimo" (vacío)
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));

        // Disparamos el evento para que Blazor actualice la pantalla
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}