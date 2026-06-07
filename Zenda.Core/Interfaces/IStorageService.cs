public interface IStorageService
{
    Task<string> SubirArchivoAsync(Stream fileStream, string fileName, string folder);
}