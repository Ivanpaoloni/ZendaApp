using System.Net.Http.Json;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;

namespace Zenda.Client.Services;

public class TurnoClient : BaseClient
{
    private readonly HttpClient _http;
    public TurnoClient(HttpClient http) => _http = http;

    public async Task<DisponibilidadFechaDto?> GetDisponibilidad(Guid prestadorId, DateTime fecha, Guid servicioId)
    {
        var fechaStr = fecha.ToString("yyyy-MM-dd");

        var response = await _http.GetFromJsonAsync<DisponibilidadFechaDto>($"api/Turnos/disponibilidad/{prestadorId}?fecha={fechaStr}&servicioId={servicioId}");
        return response;
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
    public async Task<bool> ActualizarEstado(Guid turnoId, EstadoTurnoEnum nuevoEstado)
    {
        // Enviamos el enum directamente en el cuerpo de la petición
        var response = await _http.PatchAsJsonAsync($"api/turnos/{turnoId}/estado", nuevoEstado);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception(ParseError(error)); // Usando tu BaseClient con ParseError
    }
}