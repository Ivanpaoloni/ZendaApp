using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface IPlanService
    {
        Task<List<PlanVistaDto>> ObtenerPlanesActivosAsync();
        Task<bool> PuedeAgregarProfesionalAsync();
        Task<bool> TieneRecordatoriosAutomaticosAsync();
        Task<bool> PuedeCrearTurnoAsync(Guid negocioId, DateTime fechaTurno);
        Task<SuscripcionNegocioDto?> ObtenerSuscripcionActivaByNegocioId(Guid negocioId);
    }
}