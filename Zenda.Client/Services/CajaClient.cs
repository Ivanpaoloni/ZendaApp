using System.Net;
using System.Net.Http.Json;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services
{
    public class CajaClient
    {
        private readonly HttpClient _http;

        public CajaClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<CajaDiariaDto?> GetCajaHoyAsync(Guid sedeId)
        {
            var response = await _http.GetAsync($"api/Caja/hoy/{sedeId}");

            // Si la API dice "No hay contenido" (Caja cerrada) o si temporalmente no encuentra la ruta (404)
            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            // Si hay otro error (Ej: 500 Internal Server Error), lanzamos una excepción limpia
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error de la API ({response.StatusCode}): {error}");
            }

            // Solo intentamos parsear a JSON si sabemos que fue un éxito (200 OK)
            return await response.Content.ReadFromJsonAsync<CajaDiariaDto>();
        }

        public async Task AbrirCajaAsync(Guid sedeId, decimal montoInicial)
        {
            var response = await _http.PostAsJsonAsync("api/Caja/abrir", new { SedeId = sedeId, MontoInicial = montoInicial });
            if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }
}