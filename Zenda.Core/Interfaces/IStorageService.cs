namespace Zenda.Core.Interfaces
{
    public interface IStorageService
    {
        Task<string> SubirLogoAsync(byte[] fileBytes, string extension, string negocioId);
    }
}
