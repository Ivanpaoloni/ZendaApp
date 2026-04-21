using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface IPlanService
    {
        Task<List<PlanVistaDto>> ObtenerPlanesActivosAsync();
        Task<bool> PuedeAgregarProfesionalAsync();
        Task<bool> TieneRecordatoriosAutomaticosAsync();
    }
}