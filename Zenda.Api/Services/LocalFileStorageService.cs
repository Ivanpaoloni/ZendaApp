using Zenda.Core.Interfaces;

namespace Zenda.Api.Services;

public class LocalFileStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
        _env = env;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> SubirLogoAsync(byte[] fileBytes, string extension, string negocioId)
    {
        // 1. Definimos la carpeta física en el servidor: wwwroot/uploads/logos
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

        var uploadsFolder = Path.Combine(webRoot, "uploads", "logos");
        // Si la carpeta no existe, la creamos
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // 2. Generamos el nombre del archivo (pisamos el anterior si existe)
        // Le agregamos un timestamp para evitar problemas de caché en el navegador
        var fileName = $"{negocioId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // 3. Opcional: Borrar el logo viejo de este negocio para no acumular basura
        var archivosViejos = Directory.GetFiles(uploadsFolder, $"{negocioId}_*.*");
        foreach (var viejo in archivosViejos)
        {
            File.Delete(viejo);
        }

        // 4. Guardamos el archivo físico en el disco
        await File.WriteAllBytesAsync(filePath, fileBytes);

        // 5. Armamos la URL pública para devolverla
        var request = _httpContextAccessor.HttpContext!.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";

        return $"{baseUrl}/uploads/logos/{fileName}";
    }
}