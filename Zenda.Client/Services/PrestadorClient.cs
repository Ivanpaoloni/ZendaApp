using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class PrestadorClient
{
    private readonly HttpClient _http;
    public PrestadorClient(HttpClient http) => _http = http;

    public async Task<List<PrestadorReadDto>> GetAll()
        => await _http.GetFromJsonAsync<List<PrestadorReadDto>>("api/prestadores");
}