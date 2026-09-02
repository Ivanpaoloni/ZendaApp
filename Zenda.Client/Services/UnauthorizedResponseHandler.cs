using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components;
using System.Net;
using Zenda.Client.Pages;
using Zenda.Client.Pages.Clientes;

namespace Zenda.Client.Handlers;

public class UnauthorizedResponseHandler : DelegatingHandler
{
    private readonly NavigationManager _nav;

    public UnauthorizedResponseHandler(NavigationManager nav)
    {
        _nav = nav;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var currentUri = _nav.Uri;

            // 🛡️ EXCEPCIÓN DE RUTAS PÚBLICAS:
            // Si el cliente está en el flujo de reservas o en el login, 
            // evitamos la redirección forzosa al login.
            bool esRutaPublica = currentUri.Contains("/reserva", StringComparison.OrdinalIgnoreCase) ||
                                 currentUri.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
                                 // Considera si usas la ruta raíz con slug para reservas públicas
                                 EsRutaDeSlugPublico(currentUri);

            if (!esRutaPublica)
            {
                _nav.NavigateTo("/login");
            }
        }

        return response;
    }

    private bool EsRutaDeSlugPublico(string uri)
    {
        // Opcional: Si tus URLs públicas de reservas son directamente la raíz con un slug 
        // (ej: app.zendy.com.ar/peluqueria-carlos), puedes filtrar por segmentos si es necesario.
        var uriObj = new Uri(uri);
        var segments = uriObj.Segments;

        // Si la ruta tiene un solo segmento después del dominio (ej: /peluqueria-carlos), 
        // asumimos que es una vista pública de reservas y no lo expulsamos.
        return segments.Length == 2 && !segments[1].Equals("admin", StringComparison.OrdinalIgnoreCase)
                                     && !segments[1].Equals("configuracion", StringComparison.OrdinalIgnoreCase);
    }
}