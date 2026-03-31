using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class NegociosController : ControllerBase
{
    private readonly INegocioService _service;

    public NegociosController(INegocioService service) => _service = service;

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<NegocioReadDto>> GetPublicBySlug(string slug)
    {
        var negocio = await _service.GetPublicBySlugAsync(slug);
        return negocio == null ? NotFound() : Ok(negocio);
    }

    [Authorize] // Solo el dueño logueado
    [HttpGet("perfil")]
    public async Task<ActionResult<NegocioReadDto>> GetMiNegocio()
    {
        var negocio = await _service.GetPerfilAsync();
        if (negocio == null) return NotFound();
        return Ok(negocio);
    }

    
    [HttpGet("validar-slug")]
    public async Task<ActionResult<bool>> ValidarSlug([FromQuery] string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

        var disponible = await _service.IsSlugAvailableAsync(slug);
        return Ok(disponible);
    }

    // 🎯 NUEVO: Para guardar los cambios
    [HttpPut("perfil")]
    public async Task<ActionResult> UpdateMiNegocio(NegocioUpdateDto dto)
    {
        try
        {
            var actualizado = await _service.UpdatePerfilAsync(dto);
            if (actualizado) return NoContent();

            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize] // Gestión administrativa
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NegocioReadDto>> GetById(Guid id)
    {
        var negocio = await _service.GetByIdAsync(id);
        return negocio == null ? NotFound() : Ok(negocio);
    }

    [Authorize] // Solo personal autorizado
    [HttpPost]
    public async Task<ActionResult<NegocioReadDto>> Create(NegocioCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    // 🎯 NUEVO: Endpoint exclusivo para subir el logo
    [Authorize]
    [HttpPost("perfil/logo")]
    public async Task<ActionResult> UploadLogo(IFormFile file,
        [FromServices] IStorageService storageService,
        [FromServices] ITenantService tenantService)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No se envió ningún archivo." });

        var tenantId = tenantService.GetCurrentTenantId()?.ToString();
        if (tenantId == null) return Unauthorized();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Formato no permitido." });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        try
        {
            // 1. Subimos la foto al servidor web (API)
            var urlLogo = await storageService.SubirLogoAsync(fileBytes, extension, tenantId);

            // 2. Guardamos la URL en la base de datos (PostgreSQL/Neon)
            await _service.UpdateLogoUrlAsync(urlLogo);

            return Ok(new { url = urlLogo });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno.", error = ex.Message });
        }
    }
}