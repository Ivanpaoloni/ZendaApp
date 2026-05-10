public interface IExternalCalendarAuthService
{
    string GenerarUrlOAuth(string state);
    Task<string?> CrearEventoAsync(string refreshToken, string calendarId, string titulo, string descripcion, DateTime inicioUtc, DateTime finUtc);
    Task<(string RefreshToken, string Email, string CalendarId)> IntercambiarCodigoAsync(string code);
}