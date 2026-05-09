public interface IExternalCalendarAuthService
{
    string GenerarUrlOAuth(string state);
    Task<(string RefreshToken, string Email)> IntercambiarCodigoAsync(string code);
}