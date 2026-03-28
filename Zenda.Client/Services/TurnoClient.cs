using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class TurnoClient
{
    private readonly HttpClient _http;
    public TurnoClient(HttpClient http) => _http = http;

    public async Task<DisponibilidadFechaDto?> GetDisponibilidad(Guid prestadorId, DateTime fecha)
    {
        var fechaStr = fecha.ToString("yyyy-MM-dd");
        return await _http.GetFromJsonAsync<DisponibilidadFechaDto>($"api/turnos/disponibilidad/{prestadorId}?fecha={fechaStr}");
    }

    public async Task<TurnoReadDto?> Reservar(TurnoCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/turnos", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<TurnoReadDto>();

        return null;
    }
}