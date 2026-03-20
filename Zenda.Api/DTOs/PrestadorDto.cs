namespace Zenda.Api.DTOs;

// Este es el que usamos para recibir datos (POST/PUT)
public class PrestadorCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Este es el que devolvemos (GET)
public class PrestadorReadDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class PrestadorUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}