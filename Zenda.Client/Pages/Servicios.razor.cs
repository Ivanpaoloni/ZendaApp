using Microsoft.AspNetCore.Components;
using Zenda.Client.Services;

namespace Zenda.Client.Pages;

public partial class Servicios : ComponentBase
{
    [Inject]
    private ServicioClient _servicioClient { get; set; } = default!;

    // --- ESTADO DE LA PÁGINA ---
    private List<CategoriaServicioReadDto> categorias = new();
    private bool cargando = true;
    private bool guardando = false;
    private string? mensajeError;
    private string? mensajeErrorModal;

    // --- ESTADO DE LOS MODALES ---
    private bool mostrarModalCategoria = false;
    private bool mostrarModalServicio = false;

    // --- ESTADO DEL DIALOG DE CONFIRMACIÓN ---
    private bool mostrarConfirmacion = false;
    private string tituloConfirmacion = string.Empty;
    private string mensajeConfirmacion = string.Empty;
    private Func<Task>? accionPendiente = null;

    // --- ESTADO DEL SNACKBAR DE ALERTA ---
    private bool mostrarAlerta = false;
    private string tituloAlerta = string.Empty;
    private string mensajeAlerta = string.Empty;

    // --- MODELOS DE FORMULARIO ---
    private CategoriaServicioCreateDto nuevaCategoria = new();
    private ServicioCreateDto nuevoServicio = new() { DuracionMinutos = 30, Precio = 0 };
    private Guid? categoriaEnEdicionId = null;
    private Guid? servicioEnEdicionId = null;
    protected override async Task OnInitializedAsync()
    {
        await CargarCatalogo();
    }

    private async Task CargarCatalogo()
    {
        try
        {
            cargando = true;
            mensajeError = null;
            categorias = await _servicioClient.GetCatalogo();
        }
        catch (Exception ex)
        {
            mensajeError = "No pudimos cargar tu catálogo. Por favor, intentá de nuevo.";
            Console.WriteLine($"Error GetCatalogo: {ex.Message}");
        }
        finally
        {
            cargando = false;
        }
    }

    // ==========================================
    // LÓGICA DE CONFIRMACIÓN DE ELIMINACIÓN
    // ==========================================
    
    private void PrepararEliminarServicio(Guid categoriaId, ServicioReadDto servicio)
    {
        tituloConfirmacion = "Eliminar Servicio";
        mensajeConfirmacion = $"¿Estás seguro de que querés eliminar '{servicio.Nombre}'? Esta acción no se puede deshacer.";

        accionPendiente = async () => await EliminarServicio(categoriaId, servicio.Id);
        mostrarConfirmacion = true;
    }

    private async Task ManejarRespuestaConfirmacion(bool confirmado)
    {
        mostrarConfirmacion = false;

        if (confirmado && accionPendiente != null)
        {
            await accionPendiente.Invoke();
        }

        accionPendiente = null;
    }

    // ==========================================
    // LÓGICA DE CATEGORÍAS
    // ==========================================
    private void AbrirModalCategoria(CategoriaServicioReadDto? catEditar = null)
    {
        if (catEditar == null)
        {
            categoriaEnEdicionId = null;
            nuevaCategoria = new CategoriaServicioCreateDto();
        }
        else
        {
            categoriaEnEdicionId = catEditar.Id;
            nuevaCategoria = new CategoriaServicioCreateDto { Nombre = catEditar.Nombre };
        }
        mostrarModalCategoria = true;
    }

    private void CerrarModalCategoria() => mostrarModalCategoria = false;

    private async Task GuardarCategoria()
    {
        if (string.IsNullOrWhiteSpace(nuevaCategoria.Nombre)) return;

        try
        {
            guardando = true;

            if (categoriaEnEdicionId == null)
            {
                var catCreada = await _servicioClient.CreateCategoria(nuevaCategoria);
                if (catCreada != null)
                {
                    categorias.Add(catCreada);
                    CerrarModalCategoria();
                }
            }
            else
            {
                var exito = await _servicioClient.UpdateCategoria(categoriaEnEdicionId.Value, nuevaCategoria);
                if (exito)
                {
                    var catLocal = categorias.FirstOrDefault(c => c.Id == categoriaEnEdicionId.Value);
                    if (catLocal != null) catLocal.Nombre = nuevaCategoria.Nombre;
                    CerrarModalCategoria();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al guardar categoría: {ex.Message}");
        }
        finally
        {
            guardando = false;
        }
    }

    private async Task EliminarCategoria(Guid id)
    {
        try
        {
            var exito = await _servicioClient.DeleteCategoria(id);
            if (exito)
            {
                categorias.RemoveAll(c => c.Id == id);
                mensajeError = null;
            }
        }
        catch (Exception ex)
        {
            mensajeError = ex.Message;
        }
    }

    // ==========================================
    // LÓGICA DE SERVICIOS
    // ==========================================
    private void AbrirModalServicio(Guid categoriaId, ServicioReadDto? servicioEditar = null)
    {
        if (servicioEditar == null)
        {
            servicioEnEdicionId = null;
            nuevoServicio = new ServicioCreateDto
            {
                CategoriaId = categoriaId,
                DuracionMinutos = 30,
                Precio = 0
            };
        }
        else
        {
            servicioEnEdicionId = servicioEditar.Id;
            nuevoServicio = new ServicioCreateDto
            {
                CategoriaId = categoriaId,
                Nombre = servicioEditar.Nombre,
                DuracionMinutos = servicioEditar.DuracionMinutos,
                Precio = servicioEditar.Precio
            };
        }

        mensajeErrorModal = null;
        mostrarModalServicio = true;
    }

    private void CerrarModalServicio() => mostrarModalServicio = false;

    private async Task GuardarServicio()
    {
        if (string.IsNullOrWhiteSpace(nuevoServicio.Nombre))
        {
            mensajeErrorModal = "El nombre es obligatorio.";
            return;
        }

        try
        {
            guardando = true;
            mensajeErrorModal = null;

            if (servicioEnEdicionId == null)
            {
                var servCreado = await _servicioClient.CreateServicio(nuevoServicio);
                if (servCreado != null)
                {
                    var categoriaPadre = categorias.FirstOrDefault(c => c.Id == servCreado.CategoriaId);
                    categoriaPadre?.Servicios.Add(servCreado);
                    CerrarModalServicio();
                }
            }
            else
            {
                var exito = await _servicioClient.UpdateServicio(servicioEnEdicionId.Value, nuevoServicio);
                if (exito)
                {
                    var categoriaPadre = categorias.FirstOrDefault(c => c.Id == nuevoServicio.CategoriaId);
                    var servicioLocal = categoriaPadre?.Servicios.FirstOrDefault(s => s.Id == servicioEnEdicionId.Value);

                    if (servicioLocal != null)
                    {
                        servicioLocal.Nombre = nuevoServicio.Nombre;
                        servicioLocal.DuracionMinutos = nuevoServicio.DuracionMinutos;
                        servicioLocal.Precio = nuevoServicio.Precio;
                    }
                    CerrarModalServicio();
                }
                else
                {
                    mensajeErrorModal = "No se pudo actualizar el servicio.";
                }
            }
        }
        catch (Exception ex)
        {
            mensajeErrorModal = "Ocurrió un error al guardar. Intentá nuevamente.";
            Console.WriteLine($"Error al guardar servicio: {ex.Message}");
        }
        finally
        {
            guardando = false;
        }
    }

    private async Task EliminarServicio(Guid categoriaId, Guid servicioId)
    {
        try
        {
            var exito = await _servicioClient.DeleteServicio(servicioId);
            if (exito)
            {
                var categoria = categorias.FirstOrDefault(c => c.Id == categoriaId);
                if (categoria != null)
                {
                    var servicio = categoria.Servicios.FirstOrDefault(s => s.Id == servicioId);
                    if (servicio != null)
                    {
                        categoria.Servicios.Remove(servicio);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar servicio: {ex.Message}");
        }
    }

    private void PrepararEliminarCategoria(CategoriaServicioReadDto categoria)
    {
        // 🎯 FALLO RÁPIDO: Verificamos en el cliente si tiene servicios
        if (categoria.Servicios != null && categoria.Servicios.Any())
        {
            tituloAlerta = "Categoría en uso";
            mensajeAlerta = $"No podés eliminar '{categoria.Nombre}' porque todavía tiene servicios adentro. Por favor, eliminá o mové los servicios primero.";
            mostrarAlerta = true;
            return; // Cortamos la ejecución acá
        }

        // Si está vacía, procedemos con la confirmación normal
        tituloConfirmacion = "Eliminar Categoría";
        mensajeConfirmacion = $"¿Estás seguro de que querés eliminar '{categoria.Nombre}'? Esta acción no se puede deshacer.";

        accionPendiente = async () => await EliminarCategoria(categoria.Id);
        mostrarConfirmacion = true;
    }

    // 🎯 NUEVO: Método para cerrar la alerta
    private void CerrarAlerta()
    {
        mostrarAlerta = false;
    }

    // 🎯 NUEVO: Método para que el usuario pueda limpiar el banner rojo genérico
    private void LimpiarMensajeError()
    {
        mensajeError = null;
    }
}