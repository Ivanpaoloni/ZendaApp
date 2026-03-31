using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class SedeClient : BaseClient
{
    private readonly HttpClient _http;

    public SedeClient(HttpClient http) => _http = http;

    public async Task<List<SedeReadDto>> GetPublicByNegocio(Guid negocioId)
    {
        return await _http.GetFromJsonAsync<List<SedeReadDto>>($"api/sedes/public/negocio/{negocioId}")
               ?? new List<SedeReadDto>();
    }

    public async Task<List<SedeReadDto>> GetAll()
    {
        return await _http.GetFromJsonAsync<List<SedeReadDto>>("api/sedes")
               ?? new List<SedeReadDto>();
    }

    public async Task<SedeReadDto?> Create(SedeCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/sedes", dto);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SedeReadDto>();

        return null;
    }

    // 🎯 NUEVO: Método para hacer la llamada de edición
    public async Task<bool> Update(Guid id, SedeCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/sedes/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Delete(Guid id)
    {
        var response = await _http.DeleteAsync($"api/sedes/{id}");

        if (response.IsSuccessStatusCode) return true;

        var errorContent = await response.Content.ReadAsStringAsync();

        // Intentamos extraer solo el mensaje si es un JSON de error de ASP.NET
        try
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(errorContent);
            if (jsonDoc.RootElement.TryGetProperty("message", out var msgElement))
            {
                errorContent = msgElement.GetString();
            }
        }
        catch
        {
            // Si no es JSON, nos quedamos con el texto original
        }

        throw new Exception(errorContent);
    }
}