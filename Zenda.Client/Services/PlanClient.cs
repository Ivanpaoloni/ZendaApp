using System.Net.Http.Json;

namespace Zenda.Client.Services;

public class PlanClient
{
    private readonly HttpClient _http;

    public PlanClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> PuedeAgregarProfesional()
    {
        // Usaremos la ruta api/planes
        return await _http.GetFromJsonAsync<bool>("api/planes/puede-agregar-profesional");
    }

    public async Task<bool> PuedeAgregarSede()
    {
        return await _http.GetFromJsonAsync<bool>("api/planes/puede-agregar-sede");
    }
}