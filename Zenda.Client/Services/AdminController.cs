using System.Net.Http.Json;
using Zenda.Core.DTOs.Admin;

namespace Zenda.Client.Services;

public class AdminClient
{
    private readonly HttpClient _httpClient;

    public AdminClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<NegocioAdminListDto>?> GetNegociosAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<NegocioAdminListDto>>("api/admin/negocios");
    }
    public async Task<bool> UpdateNegocioAsync(Guid negocioId, AdminUpdateNegocioDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/negocios/{negocioId}", dto);
        return response.IsSuccessStatusCode;
    }
    public async Task<List<PlanAdminDto>?> GetPlanesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<PlanAdminDto>>("api/admin/planes");
    }
}