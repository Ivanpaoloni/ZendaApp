using Resend;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly string _fromEmail = "turnos@zenda-app.com.ar"; // Tu mail de salida
    private readonly string _adminEmail = "ivanpaoloni@gmail.com"; // 👈 PONÉ TU MAIL ACÁ

    public ResendEmailService(IResend resend)
    {
        _resend = resend;
    }

    // 1. CONFIRMACIÓN DE TURNO
    public async Task<bool> EnviarConfirmacionTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, Guid turnoId)
    {
        var message = new EmailMessage
        {
            From = $"Zenda App <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"✅ ¡Turno confirmado en {nombreNegocio}!",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #4f46e5; margin-top: 0;'>¡Hola {nombreCliente}! 🎉</h2>
                    <p>Tu turno en <strong>{nombreNegocio}</strong> ha sido confirmado exitosamente. Ya te estamos esperando.</p>
                    
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 16px;'>📅 <strong>Fecha:</strong> {fechaTurno:dd/MM/yyyy}</p>
                        <p style='margin: 5px 0 0 0; font-size: 16px;'>⏰ <strong>Hora:</strong> {fechaTurno:HH:mm} hs</p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://app.zenda-app.com.ar/gestionar-turno?id={turnoId}' style='background-color: #4f46e5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                            Gestionar o Cancelar Turno
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si necesitás cancelar o modificar tu turno, por favor hacelo lo antes posible usando el botón de arriba.</p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Reservas gestionadas de forma inteligente con <strong>Zenda App</strong>
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }

    // 2. RECORDATORIO DE TURNO
    public async Task<bool> EnviarRecordatorioProximoTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, Guid turnoId)
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
                    
                    <<div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 16px;'>📅 <strong>Fecha:</strong> {fechaTurno:dd/MM/yyyy}</p>
                        <p style='margin: 5px 0 0 0; font-size: 16px;'>⏰ <strong>Hora:</strong> {fechaTurno:HH:mm} hs</p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://app.zenda-app.com.ar/gestionar-turno?id={turnoId}' style='background-color: #4f46e5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                            Gestionar o Cancelar Turno
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si necesitás cancelar o modificar tu turno, por favor hacelo lo antes posible usando el botón de arriba.</p>
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Reservas gestionadas de forma inteligente con <strong>Zenda App</strong>
                    </p>
                </div>"
        };

        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }

    // 3. BIENVENIDA AL SaaS
    public async Task<bool> EnviarBienvenidaRegistroAsync(string emailDestino, string nombreUsuario, string nombreNegocio)
    {
        var message = new EmailMessage
        {
            From = $"Equipo Zenda <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"🚀 ¡Bienvenido a Zenda, {nombreUsuario}!",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #4f46e5; margin-top: 0;'>¡Bienvenido a bordo! 🚀</h2>
                    <p>Hola <strong>{nombreUsuario}</strong>, estamos muy felices de que hayas elegido Zenda para llevar la gestión de <strong>{nombreNegocio}</strong> al siguiente nivel.</p>
                    
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 15px;'>💡 <strong>Siguiente paso:</strong> Ya podés ingresar a tu panel, crear tu primera sede y configurar a tu equipo de profesionales para empezar a recibir reservas.</p>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si tenés alguna duda o necesitás ayuda para arrancar, simplemente respondé a este correo y nos pondremos en contacto con vos.</p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        <strong>Zenda App</strong> - Creciendo junto a tu negocio
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }

    // 4. CONTACTO DESDE LA LANDING PAGE
    public async Task<bool> EnviarConsultaContactoAsync(string nombreRemitente, string emailRemitente, string mensaje)
    {
        var message = new EmailMessage
        {
            From = $"Landing Zenda <{_fromEmail}>",
            To = { _adminEmail }, // ESTE VA HACIA VOS, NO AL CLIENTE
            Subject = $"📩 Nueva consulta de {nombreRemitente} (Landing Page)",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #4f46e5; margin-top: 0;'>Tenés un nuevo mensaje web 📬</h2>
                    <p>Alguien completó el formulario de contacto en la Landing Page de Zenda.</p>
                    
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 15px;'>👤 <strong>Nombre:</strong> {nombreRemitente}</p>
                        <p style='margin: 5px 0 0 0; font-size: 15px;'>✉️ <strong>Email:</strong> {emailRemitente}</p>
                    </div>

                    <p style='font-size: 14px; color: #555;'><strong>Mensaje del usuario:</strong></p>
                    <p style='background-color: #fff; padding: 15px; border-left: 4px solid #4f46e5; border-radius: 0 4px 4px 0; box-shadow: 0 1px 3px rgba(0,0,0,0.1); font-size: 15px;'>
                        {mensaje}
                    </p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación interna de <strong>Zenda App</strong>
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
    // 5. CONFIRMACIÓN DE CANCELACIÓN
    public async Task<bool> EnviarCancelacionTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurnoLocal, string negocioSlug)
    {
        var baseUrl = "https://app.zenda-app.com.ar"; // Cambiar por localhost en dev

        var message = new EmailMessage
        {
            From = $"Zenda App <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"🚫 Turno cancelado en {nombreNegocio}",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #ef4444; margin-top: 0;'>Turno Cancelado</h2>
                    <p>Hola <strong>{nombreCliente}</strong>, te confirmamos que tu turno en <strong>{nombreNegocio}</strong> ha sido cancelado correctamente.</p>
                    
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 15px; color: #666;'><del>📅 Fecha original: {fechaTurnoLocal:dd/MM/yyyy}</del></p>
                        <p style='margin: 5px 0 0 0; font-size: 15px; color: #666;'><del>⏰ Hora original: {fechaTurnoLocal:HH:mm} hs</del></p>
                    </div>

                    <p>Entendemos que los planes cambian. Cuando estés listo para volver, podés agendar un nuevo horario haciendo clic acá:</p>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{baseUrl}/{negocioSlug}' style='background-color: #f3f4f6; color: #374151; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; border: 1px solid #d1d5db;'>
                            Agendar un nuevo turno
                        </a>
                    </div>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación automática de <strong>Zenda App</strong>
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
}