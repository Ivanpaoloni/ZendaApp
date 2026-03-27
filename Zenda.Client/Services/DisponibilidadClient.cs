using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class DisponibilidadClient
{
    private readonly HttpClient _http;
    public DisponibilidadClient(HttpClient http) => _http = http;

    public async Task<List<DisponibilidadReadDto>> GetByPrestador(Guid prestadorId)
    {
        return await _http.GetFromJsonAsync<List<DisponibilidadReadDto>>($"api/disponibilidad/prestador/{prestadorId}")
               ?? new List<DisponibilidadReadDto>();
    }

    public async Task<bool> Upsert(Guid prestadorId, List<DisponibilidadCreateDto> agenda)
    {
        var response = await _http.PostAsJsonAsync($"api/disponibilidad/upsert/{prestadorId}", agenda);
        return response.IsSuccessStatusCode;
    }
}