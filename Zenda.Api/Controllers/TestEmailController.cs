using Microsoft.AspNetCore.Mvc;
using Zenda.Core.Interfaces;

[ApiController]
[Route("api/test-email")]
public class TestEmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public TestEmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Test()
    {
        // ACORDATE: En modo prueba de Resend, 'To' tiene que ser TU MAIL
        var exito = await _emailService.EnviarConfirmacionTurnoAsync(
            "ivanpaoloni@gmail.com",
            "Ivan",
            "Zenda Test",
            DateTime.Now.AddDays(1));

        return exito ? Ok("Mail enviado!") : BadRequest("Falló el envío");
    }
}