using System.Net;
using Microsoft.AspNetCore.Components;

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

        // 🎯 EL FIX: Solo redirigimos si NO estamos ya en la página de login
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Usamos OrdinalIgnoreCase para que ataje "/login" o "/Login"
            if (!_nav.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                _nav.NavigateTo("/login");
            }
        }

        return response;
    }
}