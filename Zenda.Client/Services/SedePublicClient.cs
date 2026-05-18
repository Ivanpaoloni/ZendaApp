using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class SedePublicClient
{
    private readonly HttpClient _http;

    public SedePublicClient(HttpClient http)
    {
        _http = http;
    }

    // Endpoint público que no requiere Token JWT
    public async Task<List<SedeReadDto>> GetPublicByNegocio(Guid negocioId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<SedeReadDto>>($"api/sedes/public/negocio/{negocioId}") ?? new();
        }
        catch
        {
            return new List<SedeReadDto>();
        }
    }
}
