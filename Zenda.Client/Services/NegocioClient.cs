using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class NegocioClient
{
    private readonly HttpClient _http;
    public NegocioClient(HttpClient http) => _http = http;

    public async Task<NegocioReadDto?> GetById(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<NegocioReadDto>($"api/negocios/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener negocio: {ex.Message}");
            return null;
        }
    }
    public async Task<NegocioReadDto?> GetPerfilAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<NegocioReadDto>("api/negocios/perfil");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener perfil (posible falta de sesión): {ex.Message}");
            return null;
        }
    }
    public async Task<NegocioReadDto?> GetPublicBySlug(string slug)
    {
        try
        {
            return await _http.GetFromJsonAsync<NegocioReadDto>($"api/negocios/public/{slug}");
        }
        catch { return null; }
    }
}