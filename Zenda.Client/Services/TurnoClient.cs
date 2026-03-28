using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class TurnoClient : BaseClient
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

    public async Task<List<TurnoReadDto>?> GetByFecha(DateTime fecha)
    {
        try
        {
            // Formateamos seguro a ISO 8601 para la URL
            var fechaStr = fecha.ToString("yyyy-MM-dd");

            return await _http.GetFromJsonAsync<List<TurnoReadDto>>($"api/turnos/fecha/{fechaStr}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener turnos por fecha: {ex.Message}");
            return new List<TurnoReadDto>(); // Fallback seguro para la UI
        }
    }
}