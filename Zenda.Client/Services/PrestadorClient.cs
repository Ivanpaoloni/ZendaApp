using System.Net.Http.Json;
using Zenda.Client.Components;
using Zenda.Core.DTOs;

public class PrestadorClient : BaseClient
{
    private readonly HttpClient _http;
    public PrestadorClient(HttpClient http) => _http = http;

    public async Task<List<PrestadorReadDto>> GetPublicBySede(Guid sedeId)
    {
        return await _http.GetFromJsonAsync<List<PrestadorReadDto>>($"api/prestadores/public/sede/{sedeId}")
               ?? new List<PrestadorReadDto>();
    }
    public async Task<List<PrestadorReadDto>> GetAll()
    {
        try
        {
            // Usamos GetAsync para inspeccionar la respuesta antes de parsear
            var response = await _http.GetAsync("api/Prestadores");

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
    public async Task<List<PrestadorReadDto>?> GetBySedeId(Guid sedeId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PrestadorReadDto>>($"api/prestadores/sede/{sedeId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener prestadores por sede: {ex.Message}");
            return new List<PrestadorReadDto>();
        }
    }
    public async Task<PrestadorReadDto?> GetById(Guid id)
    {
        try
        {
            var response = await _http.GetAsync($"api/prestadores/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PrestadorReadDto>();
            }
            // Aquí podrías loguear el error (response.StatusCode)
            return null;
        }
        catch (Exception ex)
        {
            // Error de conexión o red
            Console.WriteLine($"Error de red: {ex.Message}");
            return null;
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
    public async Task<(bool Success, string ErrorMessage)> Update(Guid id, PrestadorUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/prestadores/{id}", dto);

            if (response.IsSuccessStatusCode) return (true, string.Empty);

            var error = await response.Content.ReadAsStringAsync();
            // Intentamos limpiar el JSON si viene con el formato { message: "..." }
            return (false, ParseError(error));
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
        }
    }
    public async Task<bool> Delete(Guid id)
    {
        // El interceptor añadirá el token automáticamente
        var response = await _http.DeleteAsync($"api/prestadores/{id}");

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        // Usamos el extractor de mensajes que centralizamos en la clase base
        var rawError = await response.Content.ReadAsStringAsync();
        throw new Exception(ParseError(rawError));
    }
}