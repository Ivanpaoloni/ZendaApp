using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class UsuarioClient : BaseClient
{
    private readonly HttpClient _http;

    public UsuarioClient(HttpClient http) => _http = http;

    public async Task<UsuarioPerfilDto?> GetMiPerfil()
    {
        try
        {
            return await _http.GetFromJsonAsync<UsuarioPerfilDto>("api/usuarios/me");
        }
        catch
        {
            return null; // Manejo de errores silencioso si falla la red
        }
    }

    public async Task<bool> UpdateMiPerfil(UsuarioUpdateDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/usuarios/me", dto);
        return response.IsSuccessStatusCode;
    }
}