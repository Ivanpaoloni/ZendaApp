using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Zenda.Core.Interfaces;

namespace Zenda.API.Services; // Ajustá al namespace donde pongas tus servicios de API

/// <summary>
/// Implementación de IStorageService utilizando Cloudinary para almacenamiento en la nube.
/// </summary>
public class CloudinaryStorageService : IStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryStorageService(IConfiguration configuration)
    {
        // Instanciamos el cliente de Cloudinary leyendo la URL segura del appsettings
        var cloudinaryUrl = configuration["CloudinaryUrl"];
        if (string.IsNullOrEmpty(cloudinaryUrl))
            throw new ArgumentException("Falta la configuración de CloudinaryUrl en appsettings.json");

        _cloudinary = new Cloudinary(cloudinaryUrl);
        _cloudinary.Api.Secure = true; // Forzamos HTTPS siempre
    }

    public async Task<string> SubirArchivoAsync(Stream fileStream, string fileName, string folder)
    {
        // Cloudinary permite subir directamente desde un stream
        var publicId = $"zenda/{folder}/{Guid.NewGuid()}";

        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = publicId,
            Overwrite = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception(uploadResult.Error.Message);

        return uploadResult.SecureUrl.ToString();
    }
}