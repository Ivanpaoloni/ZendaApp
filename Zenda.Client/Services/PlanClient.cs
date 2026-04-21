using System.Net.Http;
using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class PlanClient : BaseClient
{
    private readonly HttpClient _http;
    public PlanClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<PlanVistaDto>> GetPlanesAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<PlanVistaDto>>("api/planes");
            return response ?? new List<PlanVistaDto>();
        }
        catch
        {
            return new List<PlanVistaDto>();
        }
    }
}