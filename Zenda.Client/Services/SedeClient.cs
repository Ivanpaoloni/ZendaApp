using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class SedeClient
{
    private readonly HttpClient _http;
    public SedeClient(HttpClient http) => _http = http;

    public async Task<List<SedeReadDto>> GetAll()
    {
        try
        {
            // Usamos GetAsync para tener la respuesta completa y validar
            var response = await _http.GetAsync("api/sedes");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<SedeReadDto>>()
                       ?? new List<SedeReadDto>();
            }

            return new List<SedeReadDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new List<SedeReadDto>();
        }
    }
    public async Task<(bool Success, string ErrorMessage)> Create(SedeCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/sedes", dto);

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var error = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrEmpty(error) ? "Error al crear la sede" : error);
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
        }
    }
}