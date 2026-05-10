public interface IMagicLinkService
{
    string GenerarTokenIntegracion(Guid prestadorId, int expiracionHoras = 24);
    Guid? ExtraerPrestadorId(string token);
}