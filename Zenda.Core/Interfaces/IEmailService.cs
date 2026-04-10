namespace Zenda.Core.Interfaces;

public interface IEmailService
{
    Task<bool> EnviarConfirmacionTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno);

    Task<bool> EnviarBienvenidaRegistroAsync(string emailDestino, string nombreUsuario, string nombreNegocio);

    Task<bool> EnviarConsultaContactoAsync(string nombreRemitente, string emailRemitente, string mensaje);
}