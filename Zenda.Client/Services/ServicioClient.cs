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
}