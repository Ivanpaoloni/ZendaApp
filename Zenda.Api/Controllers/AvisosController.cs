using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AvisosController : ControllerBase
    {
        private readonly IAvisoService _avisoService;
        private readonly IStorageService _storageService;

        public AvisosController(IAvisoService avisoService, IStorageService storageService)
        {
            _avisoService = avisoService;
            this._storageService = storageService;
        }

        // GET: api/avisos/activo
        // Este endpoint es el que consumirá Blazor al iniciar sesión
        [HttpGet("activo")]
        public async Task<IActionResult> GetActivo()
        {
            var aviso = await _avisoService.GetAvisoActivoAsync();
            if (aviso == null) return NoContent(); // 204 si no hay anuncios que mostrar

            return Ok(aviso);
        }

        // GET: api/avisos
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var avisos = await _avisoService.GetAllAsync();
            return Ok(avisos);
        }

        // POST: api/avisos
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AvisoDto dto)
        {
            var result = await _avisoService.CreateAsync(dto);
            return Ok(result);
        }

        // PUT: api/avisos/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AvisoDto dto)
        {
            var result = await _avisoService.UpdateAsync(id, dto);
            return Ok(result);
        }

        // DELETE: api/avisos/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _avisoService.DeleteAsync(id);
            return NoContent();
        }

        // PUT: api/avisos/{id}/activar
        [HttpPut("{id}/activar")]
        public async Task<IActionResult> Activar(Guid id)
        {
            await _avisoService.ActivarAvisoAsync(id);
            return NoContent();
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo.");

            // Validamos el tamaño en el backend con file.Length
            const long maxFileSize = 1024 * 1024 * 5; // 5MB
            if (file.Length > maxFileSize)
                return BadRequest("La imagen supera el límite permitido de 5MB.");

            try
            {
                // Abrimos el stream sin parámetros de tamaño
                using var stream = file.OpenReadStream();

                // Enviamos al servicio unificado guardando en la carpeta "avisos"
                var url = await _storageService.SubirArchivoAsync(stream, file.FileName, "avisos");

                return Ok(new { Url = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al subir la imagen: {ex.Message}");
            }
        }
    }
}