namespace Zenda.Core.Interfaces
{
    public interface IPlanService
    {
        Task<bool> PuedeAgregarProfesionalAsync();
        Task<bool> TieneRecordatoriosAutomaticosAsync();
    }
}