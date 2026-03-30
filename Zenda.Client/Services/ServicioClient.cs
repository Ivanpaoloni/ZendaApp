using System.Net.Http;
using System.Net.Http.Json;
using Zenda.Core.DTOs;
using static System.Net.WebRequestMethods;

namespace Zenda.Client.Services;

public class ServicioClient : BaseClient
{
    private readonly HttpClient _http;
    public ServicioClient(HttpClient http) => _http = http;

    public async Task<List<CategoriaServicioReadDto>> GetCatalogo()
    {
        var response = await _http.GetFromJsonAsync<List<CategoriaServicioReadDto>>("api/Servicios/catalogo");
        return response;
    }

    public async Task<ServicioReadDto?> CreateServicio(ServicioCreateDto dto)
    {
        var res = await _http.PostAsJsonAsync("api/servicios", dto);
        if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<ServicioReadDto>();

        var error = await res.Content.ReadAsStringAsync();
        throw new Exception(ParseError(error));
    }

    public async Task<CategoriaServicioReadDto?> CreateCategoria(CategoriaServicioCreateDto dto)
    {
        var res = await _http.PostAsJsonAsync("api/servicios/categorias", dto);
        if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<CategoriaServicioReadDto>();

        var error = await res.Content.ReadAsStringAsync();
        throw new Exception(ParseError(error));
    }

    public async Task<List<ServicioPublicoDto>> GetServiciosPublicosPorSede(Guid sedeId)
    {
        var response = await _http.GetFromJsonAsync<List<ServicioPublicoDto>>($"api/servicios/publico/sede/{sedeId}");
        return response ?? new List<ServicioPublicoDto>();
    }
    public async Task<bool> UpdateServicio(Guid id, ServicioCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"/api/servicios/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteServicio(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/servicios/{id}");
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> UpdateCategoria(Guid id, CategoriaServicioCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"/api/servicios/categorias/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCategoria(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/servicios/categorias/{id}");

        if (!response.IsSuccessStatusCode)
        {
            // Si devuelve BadRequest, leemos el mensaje (ej: "No podés eliminar...")
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        return true;
    }
}