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

    public async Task<string> SubirLogoAsync(byte[] fileBytes, string extension, string negocioId)
    {
        using var stream = new MemoryStream(fileBytes);

        // El PublicId es el nombre/ruta del archivo sin la extensión.
        // Lo guardamos en una carpeta "zenda/logos" para mantener el panel de Cloudinary ordenado.
        var publicId = $"zenda/logos/logo_{negocioId}";

        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(publicId, stream),
            PublicId = publicId,
            Overwrite = true, // 🛡️ MEJOR PRÁCTICA: Si el negocio sube otro logo, pisa el anterior. No acumulamos basura.
            Format = extension.TrimStart('.'), // Le decimos explícitamente el formato (png, jpg, etc.)
            Transformation = new Transformation()
                                .Width(512).Height(512).Crop("fill").Gravity("auto") // Optimizamos el peso recortando en origen
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            // Solución de problemas: Si Cloudinary falla, lanzamos una excepción clara para los logs
            throw new Exception($"Error al subir imagen a Cloudinary: {uploadResult.Error.Message}");
        }

        // Devolvemos la URL segura (HTTPS) para guardar en Neon
        return uploadResult.SecureUrl.ToString();
    }
}