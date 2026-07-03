using Hangfire;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class HangfireJobService : IJobService
{
    public string ProgramarRecordatorioEmail(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, DateTime fechaEjecucion, Guid turnoId)
    {
        // No hace falta instanciar IEmailService. Solo le decimos qué método ejecutar.
        var jobId = BackgroundJob.Schedule<IEmailService>(emailService => emailService.EnviarRecordatorioProximoTurnoAsync(emailDestino, nombreCliente, nombreNegocio, fechaTurno, turnoId), fechaEjecucion);
        
        // A futuro se puede hacer un EnviarRecordatorioAsync específico

        return jobId;
    }

    public bool CancelarTrabajo(string jobId)
    {
        return BackgroundJob.Delete(jobId);
    }

}