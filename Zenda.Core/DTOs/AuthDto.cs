namespace Zenda.Application.DTOs.Auth;

// Lo que nos manda el frontend para registrarse
public class RegisterOwnerDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Datos del negocio que se crea junto con el Owner
    public string NombreNegocio { get; set; } = string.Empty;
    public string SlugNegocio { get; set; } = string.Empty; 
    public Guid RubroId { get; set; }
}

// Lo que nos manda para iniciar sesión
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Lo que le devolvemos al frontend
public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
}