public interface IMagicLinkService
{
    string GenerarTokenIntegracion(int prestadorId, int expiracionHoras = 24);
    int? ExtraerPrestadorId(string token);
}