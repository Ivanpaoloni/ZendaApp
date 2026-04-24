using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly ICajaService _cajaService;

        public CajaController(ICajaService cajaService)
        {
            _cajaService = cajaService;
        }

        [HttpGet("hoy/{sedeId}")]
        public async Task<ActionResult<CajaDiariaDto>> GetCajaHoy(Guid sedeId)
        {
            var caja = await _cajaService.GetEstadoCajaHoyAsync(sedeId);
            if (caja == null) return NoContent(); // Código 204 significa "No hay caja abierta hoy"

            return Ok(caja);
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirCaja([FromBody] AbrirCajaRequest request)
        {
            try
            {
                await _cajaService.AbrirCajaAsync(request.SedeId, request.MontoInicial);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] NuevoMovimientoDto dto)
        {
            try
            {
                await _cajaService.RegistrarMovimientoManualAsync(dto);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    // Auxiliar Dto para el request
    public class AbrirCajaRequest
    {
        public Guid SedeId { get; set; }
        public decimal MontoInicial { get; set; }
    }
}