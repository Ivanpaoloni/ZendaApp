namespace Zenda.Core.Interfaces;

public interface IJobService
{
    // Programa una tarea para el futuro y devuelve un ID (útil si después querés cancelar el turno)
    string ProgramarRecordatorioEmail(string emailDestino, string nombreCliente, string nombreNegocio, DateTime fechaTurno, DateTime fechaEjecucion, Guid turnoId);

    // Si un cliente cancela el turno, usamos esto para que no le llegue el mail
    bool CancelarTrabajo(string jobId);
}