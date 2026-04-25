using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class ClienteClient
{
    private readonly HttpClient _http;

    public ClienteClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ClienteReadDto>> GetAll()
    {
        return await _http.GetFromJsonAsync<List<ClienteReadDto>>("api/clientes") ?? new();
    }

    public async Task<List<TurnoReadDto>> GetHistorial(Guid clienteId)
    {
        return await _http.GetFromJsonAsync<List<TurnoReadDto>>($"api/clientes/{clienteId}/turnos") ?? new();
    }
    public async Task<Stream?> GetExcelStreamAsync()
    {
        var response = await _http.GetAsync("api/clientes/exportar");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync();
        }

        return null;
    }
}