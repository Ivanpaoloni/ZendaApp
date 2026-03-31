using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using Zenda.Core.DTOs;

public class NegocioClient : BaseClient
{
    private readonly HttpClient _http;

    public NegocioClient(HttpClient http) => _http = http;

    public async Task<NegocioReadDto?> GetPerfilAsync()
    {
        return await _http.GetFromJsonAsync<NegocioReadDto>("api/negocios/perfil");
    }

    public async Task<NegocioReadDto?> GetPublicBySlugAsync(string slug)
    {
        return await _http.GetFromJsonAsync<NegocioReadDto>($"api/negocios/public/{slug}");
    }

    public async Task<bool> ValidarSlugDisponible(string slug)
    {
        try
        {
            return await _http.GetFromJsonAsync<bool>($"api/negocios/validar-slug?slug={slug}");
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateMiNegocio(NegocioUpdateDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/negocios/perfil", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> SubirLogo(IBrowserFile archivo)
    {
        using var content = new MultipartFormDataContent();

        // Leemos el archivo (limitado a 2MB por seguridad en el frontend también)
        var maxFileSize = 2 * 1024 * 1024;
        using var stream = archivo.OpenReadStream(maxFileSize);
        using var streamContent = new StreamContent(stream);

        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(archivo.ContentType);

        content.Add(streamContent, "file", archivo.Name);

        var response = await _http.PostAsync("api/negocios/perfil/logo", content);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LogoResponse>();
            return result?.Url;
        }

        return null;
    }

    // Clase auxiliar para leer la respuesta JSON { "url": "..." }
    private class LogoResponse { public string Url { get; set; } = ""; }
}