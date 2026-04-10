using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Zenda.Core.Interfaces;
using Zenda.Core.DTOs;

namespace Zenda.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ContactoController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public ContactoController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensaje([FromBody] ContactoCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest("Faltan datos obligatorios.");
            }

            var exito = await _emailService.EnviarConsultaContactoAsync(dto.Nombre, dto.Email, dto.Mensaje);

            if (exito)
            {
                return Ok(new { mensaje = "Mensaje enviado correctamente." });
            }

            return StatusCode(500, "Hubo un error al enviar el mensaje. Intentá nuevamente.");
        }
    }
}