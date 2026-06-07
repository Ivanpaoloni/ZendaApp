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
    public async Task<string> SubirArchivoAsync(Stream fileStream, string fileName, string folder)
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var fileBytes = ms.ToArray();
        var extension = Path.GetExtension(fileName);

        // Reutilizamos tu lógica de guardado local pero con la firma unificada
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var targetFolder = Path.Combine(webRoot, "uploads", folder);

        if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

        var finalFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(targetFolder, finalFileName);

        await File.WriteAllBytesAsync(filePath, fileBytes);

        var request = _httpContextAccessor.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}/uploads/{folder}/{finalFileName}";
    }
}