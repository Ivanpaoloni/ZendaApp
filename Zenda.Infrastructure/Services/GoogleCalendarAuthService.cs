using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Oauth2.v2;
using Microsoft.Extensions.Configuration;

namespace Zenda.Infrastructure.Services;

public class GoogleCalendarAuthService : IExternalCalendarAuthService
{
    private readonly IConfiguration _config;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GoogleCalendarAuthService(IConfiguration config)
    {
        _config = config;
        _clientId = _config["GoogleCalendar:ClientId"] ?? throw new ArgumentNullException("Google ClientId no configurado");
        _clientSecret = _config["GoogleCalendar:ClientSecret"] ?? throw new ArgumentNullException("Google ClientSecret no configurado");
        _redirectUri = _config["GoogleCalendar:RedirectUri"] ?? throw new ArgumentNullException("Google RedirectUri no configurado");
    }

    public string GenerarUrlOAuth(string state)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            },
            Scopes = new[] { CalendarService.Scope.CalendarEvents, Oauth2Service.Scope.UserinfoEmail }
        });

        // 1. Hacemos un cast explícito a GoogleAuthorizationCodeRequestUrl
        var request = (GoogleAuthorizationCodeRequestUrl)flow.CreateAuthorizationCodeRequest(_redirectUri);

        // 2. Ahora el compilador reconocerá estas propiedades
        request.State = state;
        request.AccessType = "offline";
        request.Prompt = "consent";

        return request.Build().ToString();
    }

    // En GoogleCalendarAuthService.cs
    public async Task<(string RefreshToken, string Email, string CalendarId)> IntercambiarCodigoAsync(string code)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret }
        });

        TokenResponse tokenResponse = await flow.ExchangeCodeForTokenAsync(
            userId: "user",
            code: code,
            redirectUri: _redirectUri,
            taskCancellationToken: CancellationToken.None);

        string userEmail = await ObtenerEmailDelUsuarioAsync(tokenResponse);

        // Por ahora usamos "primary", pero lo guardamos para dar flexibilidad a futuro
        string calendarId = "primary";

        return (tokenResponse.RefreshToken, userEmail, calendarId);
    }

    // NUEVO MÉTODO para crear el evento
    public async Task<string?> CrearEventoAsync(string refreshToken, string calendarId, string titulo, string descripcion, DateTime inicioUtc, DateTime finUtc)
    {
        try
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret }
            });

            var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
            var credential = new UserCredential(flow, "user", tokenResponse);

            var service = new CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Zendy App"
            });

            var nuevoEvento = new Google.Apis.Calendar.v3.Data.Event()
            {
                Summary = titulo,
                Description = descripcion,
                Start = new EventDateTime { DateTimeRaw = inicioUtc.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                End = new EventDateTime { DateTimeRaw = finUtc.ToString("yyyy-MM-ddTHH:mm:ssZ") },
            };

            var request = service.Events.Insert(nuevoEvento, calendarId);
            var creado = await request.ExecuteAsync();
            return creado.Id;
        }
        catch (Exception ex)
        {
            // Loggear error (Serilog/NLog)
            return null;
        }
    }

    private async Task<string> ObtenerEmailDelUsuarioAsync(TokenResponse tokenResponse)
    {
        try
        {
            var credentials = new UserCredential(new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret }
                }), "user", tokenResponse);

            var oauth2Service = new Oauth2Service(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = "Zendy App"
            });

            var userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();
            return userInfo.Email;
        }
        catch
        {
            return "email_no_disponible@zendy.com"; // Fallback seguro
        }
    }
}