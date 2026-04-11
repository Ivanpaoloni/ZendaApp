namespace Zenda.Core.Interfaces;

public interface IEmailService
{
    Task<bool> EnviarConfirmacionTurnoAsync(
   string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, Guid turnoId,
   string servicioNombre, string profesionalNombre, string sedeNombre, string sedeDireccion);
    Task<bool> EnviarBienvenidaRegistroAsync(string emailDestino, string nombreUsuario, string nombreNegocio);

    Task<bool> EnviarConsultaContactoAsync(string nombreRemitente, string emailRemitente, string mensaje);
    Task<bool> EnviarRecordatorioProximoTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, Guid turnoId);
    Task<bool> EnviarCancelacionTurnoAsync(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurnoLocal, string negocioSlug);
}