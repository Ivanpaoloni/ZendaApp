using System.Net.Http;
using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class FacturacionClient : BaseClient
{
    private readonly HttpClient _http;

    public FacturacionClient(HttpClient http)
    {
        _http = http;
    }


    public async Task<FacturacionDto> GetResumenAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<FacturacionDto>("api/facturacion/resumen");
            return response ?? new FacturacionDto();
        }
        catch
        {
            return new FacturacionDto(); // En caso de error devolvemos DTO vacío para no romper la UI
        }
    }
}