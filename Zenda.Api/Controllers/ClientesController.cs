using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Exclusivo para dueños/recepcionistas logueados
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteReadDto>>> GetAll()
    {
        var clientes = await _clienteService.GetAllAsync();
        return Ok(clientes);
    }

    [HttpGet("{id}/turnos")]
    public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetHistorial(Guid id)
    {
        try
        {
            var historial = await _clienteService.GetHistorialTurnosAsync(id);
            return Ok(historial);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // Retorna 403 si intentan ver un cliente de otro local
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}