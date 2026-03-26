using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class PrestadorClient
{
    private readonly HttpClient _http;
    public PrestadorClient(HttpClient http) => _http = http;

    public async Task<List<PrestadorReadDto>> GetAll()
    {
        try
        {
            // Usamos GetAsync para inspeccionar la respuesta antes de parsear
            var response = await _http.GetAsync("api/prestadores");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PrestadorReadDto>>()
                       ?? new List<PrestadorReadDto>();
            }

            // Aquí podrías loguear el error (response.StatusCode)
            return new List<PrestadorReadDto>();
        }
        catch (Exception ex)
        {
            // Error de conexión o red
            Console.WriteLine($"Error de red: {ex.Message}");
            return new List<PrestadorReadDto>();
        }
    }

    public async Task<(bool Success, string ErrorMessage)> Create(PrestadorCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/prestadores", dto);

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            // Intentamos leer el error detallado del backend (si usas un Middleware de excepciones)
            var error = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrEmpty(error) ? "Error inesperado en el servidor" : error);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con el servidor: {ex.Message}");
        }
    }
}