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
        // Forzamos el formato yyyy-MM-dd para evitar problemas de Model Binding en el Backend
        var url = $"api/reportes/dashboard?inicio={inicio:yyyy-MM-dd}&fin={fin:yyyy-MM-dd}";

        return await _httpClient.GetFromJsonAsync<ReporteDashboardDto>(url);
    }

    public async Task<Stream?> GetExcelStreamAsync(DateTime inicio, DateTime fin)
    {
        // Aplicamos la misma limpieza acá por las dudas
        var url = $"api/reportes/exportar?inicio={inicio:yyyy-MM-dd}&fin={fin:yyyy-MM-dd}";

        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync();
        }
        return null;
    }
}