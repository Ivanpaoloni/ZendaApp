using Microsoft.AspNetCore.Components;
using Zenda.Client.Services;
using Zenda.Core.DTOs;

namespace Zenda.Client.Pages.Sedes;

public partial class Sedes : ComponentBase
{
    [Inject] private SedeClient SedeService { get; set; } = default!;
    [Inject] private AppState State { get; set; } = default!; 
    [Inject] private NavigationManager Nav { get; set; } = default!; 

    // 🎯 Variables para el control de plan
    protected bool puedeAgregarMas = true;
    protected bool mostrarModalUpgrade = false;

    // --- ESTADO GENERAL ---
    protected List<SedeReadDto>? sedes;
    protected string? errorMessage;

    // --- ESTADO DEL MODAL DE FORMULARIO ---
    protected bool mostrarModalSede = false;
    protected bool isSubmitting = false;
    protected string? mensajeErrorModal;

    // Modelos
    protected SedeCreateDto nuevaSede = new();
    protected Guid? sedeEnEdicionId = null;
    protected IReadOnlyCollection<TimeZoneInfo> zonasHorarias = new List<TimeZoneInfo>();

    // --- ESTADOS DE LOS DIALOGS ---
    protected bool mostrarConfirmacion = false;
    protected string tituloConfirmacion = string.Empty;
    protected string mensajeConfirmacion = string.Empty;
    private Func<Task>? accionPendiente = null;

    protected bool mostrarAlerta = false;
    protected string tituloAlerta = string.Empty;
    protected string mensajeAlerta = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        zonasHorarias = TimeZoneInfo.GetSystemTimeZones();
        await LoadSedes();

        if (sedes != null && State.CurrentNegocio != null)
        {
            puedeAgregarMas = sedes.Count < State.CurrentNegocio.MaxSedes;
        }
    }

    private async Task LoadSedes()
    {
        sedes = await SedeService.GetAll();
    }

    protected void ManejarClickNuevo()
    {
        if (puedeAgregarMas)
        {
            Nav.NavigateTo("nueva-sede"); // O la ruta que uses para crear sede
        }
        else
        {
            mostrarModalUpgrade = true;
        }
    }
    protected void LimpiarError() => errorMessage = null;

    // ==========================================
    // LÓGICA DEL FORMULARIO (CREAR/EDITAR)
    // ==========================================
    protected void AbrirModalSede(SedeReadDto? sedeEditar = null)
    {
        mensajeErrorModal = null;

        if (sedeEditar == null)
        {
            // MODO CREAR
            sedeEnEdicionId = null;
            nuevaSede = new SedeCreateDto
            {
                ZonaHorariaId = "America/Argentina/Buenos_Aires"
            };
        }
        else
        {
            // MODO EDITAR
            sedeEnEdicionId = sedeEditar.Id;
            nuevaSede = new SedeCreateDto
            {
                Nombre = sedeEditar.Nombre,
                Direccion = sedeEditar.Direccion,
                ZonaHorariaId = sedeEditar.ZonaHorariaId
            };
        }

        mostrarModalSede = true;
    }

    protected void CerrarModalSede() => mostrarModalSede = false;

    protected async Task GuardarSede()
    {
        isSubmitting = true;
        mensajeErrorModal = null;

        try
        {
            if (sedeEnEdicionId == null)
            {
                // CREACIÓN
                var result = await SedeService.Create(nuevaSede);
                if (result != null)
                {
                    await LoadSedes();
                    CerrarModalSede();
                }
                else
                {
                    mensajeErrorModal = "No se pudo crear la sede. Revisá tu conexión.";
                }
            }
            else
            {
                // EDICIÓN (Asumiendo que tu SedeClient tiene un método Update)
                // Si aún no lo tenés, este es un buen momento para agregarlo a SedeClient
                var result = await SedeService.Update(sedeEnEdicionId.Value, nuevaSede);
                if (result)
                {
                    await LoadSedes();
                    CerrarModalSede();
                }
                else
                {
                    mensajeErrorModal = "No se pudo actualizar la sede.";
                }
            }
        }
        catch (Exception ex)
        {
            mensajeErrorModal = $"Error: {ex.Message}";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    // ==========================================
    // LÓGICA DE ELIMINACIÓN
    // ==========================================
    protected void PrepararEliminarSede(SedeReadDto sede)
    {
        // Limpiamos errores previos de pantalla
        LimpiarError();

        tituloConfirmacion = "Eliminar Sede";
        mensajeConfirmacion = $"¿Estás seguro de que querés eliminar la sede '{sede.Nombre}'? Si tiene profesionales asignados, no se podrá borrar.";

        accionPendiente = async () => await EjecutarEliminar(sede.Id);
        mostrarConfirmacion = true;
    }

    private async Task EjecutarEliminar(Guid sedeId)
    {
        try
        {
            await SedeService.Delete(sedeId);
            await LoadSedes(); // Recargamos si fue exitoso
        }
        catch (Exception ex)
        {
            // 🎯 MAGIA: Atrapamos la excepción de negocio y la mostramos en un popup lindo
            tituloAlerta = "No se puede eliminar";
            mensajeAlerta = ex.Message; // "No se puede eliminar una sede con profesionales asociados."
            mostrarAlerta = true;
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

    protected void CerrarAlerta() => mostrarAlerta = false;
}