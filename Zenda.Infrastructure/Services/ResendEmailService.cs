using Resend;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly string _fromEmail = "onboarding@resend.dev"; // Tu mail de salida
    private readonly string _adminEmail = "ivanpaoloni@gmail.com"; // 👈 PONÉ TU MAIL ACÁ

    public ResendEmailService(IResend resend)
    {
        _resend = resend;
    }

    // 1. CONFIRMACIÓN DE TURNO
    public async Task<bool> EnviarConfirmacionTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno)
    {
        var message = new EmailMessage
        {
            From = $"Zenda App <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"¡Turno confirmado en {nombreNegocio}!",
            HtmlBody = $@"
                <div style='font-family: sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #166534;'>¡Hola {nombreCliente}!</h2>
                    <p>Tu turno en <strong>{nombreNegocio}</strong> ha sido confirmado exitosamente.</p>
                    <p>📅 <strong>Fecha y hora:</strong> {fechaTurno:dd/MM/yyyy HH:mm} hs</p>
                    <hr style='border: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #888;'>Gestionado con Zenda App</p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
    public async Task<bool> EnviarRecordatorioProximoTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno)
    {
        var message = new EmailMessage
        {
            From = $"Zenda App <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"⏰ Recordatorio: Tu turno en {nombreNegocio} es en breve",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #4f46e5; margin-top: 0;'>¡Hola {nombreCliente}! 👋</h2>
                    <p>Te escribimos para recordarte que tenés un turno reservado en <strong>{nombreNegocio}</strong> muy pronto.</p>
                    
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 16px;'>📅 <strong>Fecha:</strong> {fechaTurno:dd/MM/yyyy}</p>
                        <p style='margin: 5px 0 0 0; font-size: 16px;'>⏰ <strong>Hora:</strong> {fechaTurno:HH:mm} hs</p>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Te pedimos por favor puntualidad. Si por algún motivo no podés asistir, te agradecemos que te comuniques con el negocio lo antes posible para liberar el espacio.</p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Reservas gestionadas de forma inteligente con <strong>Zenda App</strong>
                    </p>
                </div>"
        };

        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }

    // 2. BIENVENIDA AL SaaS
    public async Task<bool> EnviarBienvenidaRegistroAsync(string emailDestino, string nombreUsuario, string nombreNegocio)
    {
        var message = new EmailMessage
        {
            From = $"Equipo Zenda <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"¡Bienvenido a Zenda, {nombreUsuario}!",
            HtmlBody = $@"
                <div style='font-family: sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #166534;'>¡Bienvenido a bordo! 🚀</h2>
                    <p>Hola {nombreUsuario}, estamos felices de que hayas elegido Zenda para gestionar <strong>{nombreNegocio}</strong>.</p>
                    <p>Ya podés ingresar a tu panel y configurar a tu equipo de profesionales.</p>
                    <br/>
                    <p>Si tenés dudas, respondé este correo y te ayudamos.</p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }

    // 3. CONTACTO DESDE LA LANDING PAGE
    public async Task<bool> EnviarConsultaContactoAsync(string nombreRemitente, string emailRemitente, string mensaje)
    {
        var message = new EmailMessage
        {
            From = $"Landing Zenda <{_fromEmail}>",
            To = { _adminEmail }, // ESTE VA HACIA VOS, NO AL CLIENTE
            Subject = $"Nueva consulta de {nombreRemitente} (Landing Page)",
            HtmlBody = $@"
                <div style='font-family: sans-serif; padding: 20px; color: #333;'>
                    <h2>Tenés un nuevo mensaje desde la web</h2>
                    <p><strong>Nombre:</strong> {nombreRemitente}</p>
                    <p><strong>Email:</strong> {emailRemitente}</p>
                    <hr style='border: 1px solid #eee; margin: 20px 0;' />
                    <p><strong>Mensaje:</strong></p>
                    <p style='background-color: #f9f9f9; padding: 15px; border-left: 4px solid #166534;'>
                        {mensaje}
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
}