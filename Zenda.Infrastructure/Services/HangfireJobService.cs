using Hangfire;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class HangfireJobService : IJobService
{
    public string ProgramarRecordatorioEmail(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, DateTime fechaEjecucion, Guid turnoId)
    {
        // Hangfire es tan inteligente que sabe buscar IEmailService en tu contenedor de inyección
        // No hace falta instanciarlo acá. Solo le decimos qué método ejecutar.

        var jobId = BackgroundJob.Schedule<IEmailService>(
            emailService => emailService.EnviarRecordatorioProximoTurnoAsync(emailDestino, nombreCliente, nombreNegocio, fechaTurno, turnoId), // Acá a futuro podés hacer un EnviarRecordatorioAsync específico
            fechaEjecucion
        );

        return jobId;
    }

    public bool CancelarTrabajo(string jobId)
    {
        return BackgroundJob.Delete(jobId);
    }

}