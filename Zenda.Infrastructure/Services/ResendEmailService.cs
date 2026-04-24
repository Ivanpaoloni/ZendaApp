using Resend;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly string _fromEmail = "no-reply@zendy.com.ar";
    private readonly string _adminEmail = "ivanpaoloni@gmail.com";

    public ResendEmailService(IResend resend)
    {
        _resend = resend;
    }

    // 1. CONFIRMACIÓN DE TURNO
    public async Task<bool> EnviarConfirmacionTurnoAsync(
    string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, Guid turnoId,
    string servicioNombre, string profesionalNombre, string sedeNombre, string sedeDireccion)
    {
        var baseUrl = "https://app.zendy.com.ar";

        var message = new EmailMessage
        {
            From = $"Zendy <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"✅ Turno confirmado en {nombreNegocio}",
            HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 12px;'>
                <h2 style='color: #4f46e5; margin-top: 0;'>¡Hola {nombreCliente}! 🎉</h2>
                <p>Tu reserva en <strong>{nombreNegocio}</strong> ha sido confirmada.</p>
                
                <div style='background-color: #f8fafc; padding: 20px; border-radius: 12px; margin: 20px 0; border: 1px solid #e2e8f0;'>
                    <h3 style='margin-top: 0; font-size: 14px; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em;'>Detalles del turno</h3>
                    
                    <p style='margin: 10px 0; font-size: 16px;'>📅 <strong>Fecha:</strong> {fechaTurno:dd/MM/yyyy}</p>
                    <p style='margin: 10px 0; font-size: 16px;'>⏰ <strong>Hora:</strong> {fechaTurno:HH:mm} hs</p>
                    <p style='margin: 10px 0; font-size: 16px;'>🏷️ <strong>Servicio:</strong> {servicioNombre}</p>
                    <p style='margin: 10px 0; font-size: 16px;'>👤 <strong>Profesional:</strong> {profesionalNombre}</p>
                    <p style='margin: 10px 0; font-size: 16px;'>📍 <strong>Sede:</strong> {sedeNombre} ({sedeDireccion})</p>
                </div>

                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{baseUrl}/gestionar-turno?id={turnoId}' style='background-color: #4f46e5; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>
                        Gestionar o Cancelar Turno
                    </a>
                </div>

                <p style='font-size: 13px; color: #64748b; text-align: center;'>Si necesitás hacer algún cambio, podés gestionarlo desde el botón de arriba.</p>
            
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación automática de <strong>Zendy</strong>
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
            From = $"Zendy <{_fromEmail}>",
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

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://app.zendy.com.ar/gestionar-turno?id={turnoId}' style='background-color: #4f46e5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                            Gestionar o Cancelar Turno
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si necesitás cancelar o modificar tu turno, por favor hacelo lo antes posible usando el botón de arriba.</p>
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación automática de <strong>Zendy</strong>
                    </p>
                </div>"
        };

        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }

    // 3. BIENVENIDA AL SaaS (ACTUALIZADO CON LINK DE CONFIRMACIÓN)
    public async Task<bool> EnviarBienvenidaRegistroAsync(string emailDestino, string nombreUsuario, string nombreNegocio, string confirmLink)
    {
        var message = new EmailMessage
        {
            From = $"Zendy <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"🚀 ¡Bienvenido a Zendy, {nombreUsuario}! Activá tu cuenta",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #166534; margin-top: 0;'>¡Bienvenido a bordo! 🚀</h2>
                    <p>Hola <strong>{nombreUsuario}</strong>, estamos muy felices de que hayas elegido Zendy para llevar la gestión de <strong>{nombreNegocio}</strong> al siguiente nivel.</p>
                    
                    <div style='background-color: #f7faf6; padding: 20px; border-radius: 8px; margin: 25px 0; border: 1px solid #dcfce7; text-align: center;'>
                        <p style='margin: 0 0 15px 0; font-size: 15px; color: #166534;'><strong>Para proteger tu cuenta y habilitar todas las funciones, necesitamos que confirmes tu correo electrónico.</strong></p>
                        <a href='{confirmLink}' style='background-color: #166534; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; box-shadow: 0 4px 6px -1px rgba(22, 101, 52, 0.2);'>
                            Confirmar mi correo electrónico
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si el botón no funciona, copiá y pegá este enlace en tu navegador:</p>
                    <p style='font-size: 12px; color: #6b7280; word-break: break-all;'>{confirmLink}</p>

                    <p style='font-size: 14px; color: #555; margin-top: 30px;'>Si tenés alguna duda o necesitás ayuda para arrancar, simplemente respondé a este correo.</p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación automática de <strong>Zendy</strong>
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
        var baseUrl = "https://app.zendy.com.ar";

        var message = new EmailMessage
        {
            From = $"Zendy <{_fromEmail}>",
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
                        Notificación automática de <strong>Zendy</strong>
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
    // 6. REENVÍO DE CONFIRMACIÓN DE EMAIL (NUEVO)
    public async Task<bool> EnviarEmailConfirmacionAsync(string emailDestino, string nombreUsuario, string confirmLink)
    {
        var message = new EmailMessage
        {
            From = $"Zendy <{_fromEmail}>",
            To = { emailDestino },
            Subject = $"🔒 Zendy: Verificá tu correo electrónico",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #166534; margin-top: 0;'>Verificación de cuenta</h2>
                    <p>Hola <strong>{nombreUsuario}</strong>,</p>
                    <p>Recibimos una solicitud para verificar tu dirección de correo electrónico en Zendy. Por favor, hacé clic en el botón de abajo para confirmar que este correo te pertenece.</p>
                    
                    <div style='text-align: center; margin: 35px 0;'>
                        <a href='{confirmLink}' style='background-color: #166534; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; box-shadow: 0 4px 6px -1px rgba(22, 101, 52, 0.2);'>
                            Verificar mi cuenta
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si no solicitaste esto o no tenés una cuenta en Zendy, podés ignorar este mensaje.</p>
                    <p style='font-size: 12px; color: #6b7280; word-break: break-all; margin-top: 20px;'>Link alternativo: <br>{confirmLink}</p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación automática de <strong>Zendy</strong>
                    </p>
                </div>"
        };
        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
    // 7. RECUPERACIÓN DE CONTRASEÑA
    public async Task<bool> EnviarRecuperacionClaveAsync(string email, string resetLink)
    {
        var message = new EmailMessage
        {
            From = $"Zendy <{_fromEmail}>",
            To = { email },
            Subject = "🔒 Zendy: Recuperá tu contraseña",
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333; border: 1px solid #eaeaea; border-radius: 10px;'>
                    <h2 style='color: #166534; margin-top: 0; text-align: center;'>Recuperá tu acceso</h2>
                    <p>Hola,</p>
                    <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en Zendy. Hacé clic en el botón de abajo para crear una nueva.</p>
                    
                    <div style='text-align: center; margin: 35px 0;'>
                        <a href='{resetLink}' style='background-color: #166534; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; box-shadow: 0 4px 6px -1px rgba(22, 101, 52, 0.2);'>
                            Restablecer mi contraseña
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>Si no solicitaste este cambio, podés ignorar este correo de forma segura.</p>
                    <p style='font-size: 12px; color: #6b7280; word-break: break-all; margin-top: 20px;'>Link alternativo: <br>{resetLink}</p>
                    
                    <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin-bottom: 0;'>
                        Notificación automática de <strong>Zendy</strong>
                    </p>
                </div>"
        };

        var response = await _resend.EmailSendAsync(message);
        return response.Success;
    }
}