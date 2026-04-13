using Microsoft.AspNetCore.Components;
using Zenda.Client.Services;
using Zenda.Core.DTOs;

namespace Zenda.Client.Pages.Prestadores;

public partial class Prestadores : ComponentBase
{

    [Inject] private PrestadorClient PrestadorService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AppState State { get; set; } = default!;

    protected List<PrestadorReadDto>? prestadores;
    protected string? errorMessage;

    protected bool mostrarConfirmacion = false;
    protected string tituloConfirmacion = string.Empty;
    protected string mensajeConfirmacion = string.Empty;
    private Func<Task>? accionPendiente = null;
    protected bool puedeAgregarMas = true;
    protected bool mostrarModalUpgrade = false;

    protected override async Task OnInitializedAsync()
    {
        await CargarPrestadores(); 
        
        if (prestadores != null && State.CurrentNegocio != null)
        {
            puedeAgregarMas = prestadores.Count < State.CurrentNegocio.MaxProfesionales;
        }
    }
    protected void ManejarClickNuevo()
    {
        if (puedeAgregarMas)
        {
            Nav.NavigateTo("nuevo-prestador");
        }
        else
        {
            mostrarModalUpgrade = true;
        }
    }
    private async Task CargarPrestadores()
    {
        prestadores = await PrestadorService.GetAll();
    }

    protected void IrAAgenda(PrestadorReadDto p)
    {
        State.PrestadorEnEdicion = p;
        Nav.NavigateTo("prestadores/agenda");
    }

    protected void LimpiarError()
    {
        errorMessage = null;
    }

    protected void PrepararEliminar(PrestadorReadDto p)
    {
        errorMessage = null;
        tituloConfirmacion = "Eliminar Profesional";
        mensajeConfirmacion = $"¿Estás seguro de que querés eliminar a '{p.Nombre}'? Ya no podrá recibir turnos nuevos, pero su historial se mantendrá intacto.";

        accionPendiente = async () => await EjecutarEliminar(p.Id);
        mostrarConfirmacion = true;
    }

    private async Task EjecutarEliminar(Guid prestadorId)
    {
        try
        {
            var resultado = await PrestadorService.Delete(prestadorId);
            if (resultado)
            {
                await CargarPrestadores();
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }

    protected async Task ManejarRespuestaConfirmacion(bool confirmado)
    {
        mostrarConfirmacion = false;

        if (confirmado && accionPendiente != null)
        {
            await accionPendiente.Invoke();
        }

        accionPendiente = null;
    }
}