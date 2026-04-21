using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http;
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
    // 🎯 NUEVO MÉTODO PARA MERCADOPAGO
    public async Task<GenerarLinkResponseDto?> GenerarLinkDePagoAsync(GenerarLinkDto request)
    {
        try
        {
            // Hacemos el POST al controlador MercadoPagoController
            var response = await _http.PostAsJsonAsync("api/MercadoPago/generar-link", request);

            if (response.IsSuccessStatusCode)
            {
                // Si todo sale bien, leemos la URL que nos devolvió la API
                return await response.Content.ReadFromJsonAsync<GenerarLinkResponseDto>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private class CheckoutResponse { public string? UrlCheckout { get; set; } }
    private class LogoResponse { public string Url { get; set; } = ""; }
}