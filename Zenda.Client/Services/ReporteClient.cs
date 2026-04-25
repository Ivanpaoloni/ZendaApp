using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services;

public class ReporteClient
{
    private readonly HttpClient _httpClient;

    public ReporteClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReporteDashboardDto?> GetDashboardMetricsAsync(DateTime inicio, DateTime fin)
    {
        // Pasamos las fechas en formato ISO 8601 (O) para que el backend las parsee correctamente
        var url = $"api/reportes/dashboard?inicio={inicio:O}&fin={fin:O}";
        return await _httpClient.GetFromJsonAsync<ReporteDashboardDto>(url);
    }

    public async Task<Stream?> GetExcelStreamAsync(DateTime inicio, DateTime fin)
    {
        var url = $"api/reportes/exportar?inicio={inicio:O}&fin={fin:O}";
        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync();
        }
        return null;
    }
}