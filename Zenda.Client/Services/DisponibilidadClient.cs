using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services; // Ajustá al namespace de tu proyecto

public class DisponibilidadClient
{
    private readonly HttpClient _http;

    public DisponibilidadClient(HttpClient http)
    {
        _http = http;
    }

    // --- MÉTODOS DE AGENDA SEMANAL (Los que ya tenías) ---
    
    public async Task<IEnumerable<DisponibilidadReadDto>?> GetByPrestador(Guid prestadorId)
    {
        return await _http.GetFromJsonAsync<IEnumerable<DisponibilidadReadDto>>($"api/disponibilidad/prestador/{prestadorId}");
    }

    public async Task<bool> Upsert(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda)
    {
        var res = await _http.PostAsJsonAsync($"api/disponibilidad/upsert/{prestadorId}", agenda);
        return res.IsSuccessStatusCode;
    }

    // --- NUEVOS MÉTODOS DE BLOQUEOS (Excepciones) ---

    public async Task<List<BloqueoReadDto>> GetBloqueos(Guid prestadorId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<BloqueoReadDto>>($"api/disponibilidad/bloqueos/{prestadorId}") 
                   ?? new List<BloqueoReadDto>();
        }
        catch
        {
            return new List<BloqueoReadDto>();
        }
    }

    public async Task<bool> CrearBloqueo(BloqueoCreateDto dto)
    {
        var res = await _http.PostAsJsonAsync("api/disponibilidad/bloqueos", dto);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> EliminarBloqueo(Guid id)
    {
        var res = await _http.DeleteAsync($"api/disponibilidad/bloqueos/{id}");
        return res.IsSuccessStatusCode;
    }
}