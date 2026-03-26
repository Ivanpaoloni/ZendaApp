using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class SedeClient
{
    private readonly HttpClient _http;
    public SedeClient(HttpClient http) => _http = http;

    public async Task<List<SedeReadDto>> GetAll()
        => await _http.GetFromJsonAsync<List<SedeReadDto>>("api/sedes") ?? new();
}