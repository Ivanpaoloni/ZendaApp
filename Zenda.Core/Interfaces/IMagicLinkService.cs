public interface IMagicLinkService
{
    string GenerarTokenIntegracion(Guid prestadorId, int expiracionHoras = 24);
    Guid? ValidarTokenIntegracion(string token);

}
