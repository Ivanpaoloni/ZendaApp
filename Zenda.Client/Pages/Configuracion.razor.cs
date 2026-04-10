using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Timers;
using Zenda.Client.Services;
using Zenda.Core.DTOs;

namespace Zenda.Client.Pages;

public partial class Configuracion : ComponentBase, IDisposable
{
    [CascadingParameter] protected Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    [Inject] protected UsuarioClient UsuarioService { get; set; } = default!;
    [Inject] protected NegocioClient NegocioService { get; set; } = default!;
    [Inject] protected AppState State { get; set; } = default!;

    protected string pestañaActiva = "perfil";
    protected bool cargando = true;
    protected bool guardando = false;
    protected string? mensajeExito;
    protected string? mensajeError;

    protected UsuarioUpdateDto perfilUsuario = new();
    protected string emailUsuario = string.Empty;

    protected NegocioUpdateDto perfilNegocio = new();
    protected string slugOriginal = string.Empty;
    protected bool validandoSlug = false;
    protected bool? slugDisponible = null; // null = sin validar, true = libre, false = ocupado
    private System.Timers.Timer? debounceTimer; // El reloj para esperar que deje de teclear
    protected string? logoPreviewUrl;
    protected IBrowserFile? logoSeleccionado;
    protected bool subiendoLogo = false;

    //Hardcodeo de rubros
    private List<RubroOption> rubrosDisponibles = new()
    {
        new RubroOption { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Nombre = "Barbería" },
        new RubroOption { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Nombre = "Peluquería" },
        new RubroOption { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Nombre = "Centro de Estética" },
        new RubroOption { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Nombre = "Manicura y Pedicura" }
    };

    private class RubroOption
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = "";
    }
    protected override async Task OnInitializedAsync()
    {
        // Configuramos el timer una sola vez. Espera 600ms.
        debounceTimer = new System.Timers.Timer(600);
        debounceTimer.Elapsed += ValidarSlugEnApi;
        debounceTimer.AutoReset = false; // Que corra una sola vez por cada reseteo

        await CargarDatos();
    }

    private async Task CargarDatos()
    {
        try
        {
            cargando = true;

            var taskUsuario = UsuarioService.GetMiPerfil();
            var taskNegocio = NegocioService.GetPerfilAsync();

            await Task.WhenAll(taskUsuario, taskNegocio);

            var usuarioDb = taskUsuario.Result;
            if (usuarioDb != null)
            {
                emailUsuario = usuarioDb.Email;
                perfilUsuario.Nombre = usuarioDb.Nombre;
                perfilUsuario.Apellido = usuarioDb.Apellido;
                perfilUsuario.Telefono = usuarioDb.Telefono ?? "";
            }

            var negocioDb = taskNegocio.Result;
            if (negocioDb != null)
            {
                perfilNegocio.Nombre = negocioDb.Nombre;
                perfilNegocio.Slug = negocioDb.Slug;
                perfilNegocio.LogoUrl = negocioDb.LogoUrl;

                // NUEVO: Mapeamos los datos de negocio y reservas
                perfilNegocio.RubroId = negocioDb.RubroId;
                perfilNegocio.AnticipacionMinimaHoras = negocioDb.AnticipacionMinimaHoras;
                perfilNegocio.IntervaloTurnosMinutos = negocioDb.IntervaloTurnosMinutos;

                slugOriginal = negocioDb.Slug;
                slugDisponible = true;
            }

        }
        catch (Exception)
        {
            mensajeError = "Ocurrió un error al cargar la información.";
        }
        finally
        {
            cargando = false;
        }
    }

    protected void CambiarPestaña(string pestaña)
    {
        if (pestañaActiva == pestaña) return;
        pestañaActiva = pestaña;
        mensajeExito = null;
        mensajeError = null;
    }

    // --- LÓGICA DE PERFIL (Igual a como la tenías) ---
    protected async Task GuardarPerfil()
    {
        guardando = true;
        mensajeExito = null;
        mensajeError = null;
        try
        {
            var exito = await UsuarioService.UpdateMiPerfil(perfilUsuario);
            if (exito) mensajeExito = "¡Tus datos personales se guardaron correctamente!";
            else mensajeError = "No pudimos guardar los cambios. Intentá de nuevo.";
        }
        catch { mensajeError = "Ocurrió un error al guardar."; }
        finally { guardando = false; }
    }

    protected string ObtenerIniciales()
    {
        var iniciales = "";
        if (!string.IsNullOrWhiteSpace(perfilUsuario.Nombre)) iniciales += perfilUsuario.Nombre[0];
        if (!string.IsNullOrWhiteSpace(perfilUsuario.Apellido)) iniciales += perfilUsuario.Apellido[0];
        return string.IsNullOrEmpty(iniciales) ? "U" : iniciales.ToUpper();
    }

    // --- 🎯 NUEVA LÓGICA DE NEGOCIO ---
    protected void OnSlugInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? "";

        // Forzamos el formato: minúsculas, sin espacios (los cambiamos por guiones)
        perfilNegocio.Slug = value.ToLower().Replace(" ", "-");

        // Detenemos el reloj si venía corriendo
        debounceTimer?.Stop();

        if (string.IsNullOrWhiteSpace(perfilNegocio.Slug))
        {
            slugDisponible = false;
            return;
        }

        // Si volvió a escribir su slug original, no hace falta ir a la base de datos
        if (perfilNegocio.Slug == slugOriginal)
        {
            slugDisponible = true;
            validandoSlug = false;
            return;
        }

        // Mostramos el spinner
        validandoSlug = true;
        slugDisponible = null;

        // Arrancamos el reloj. Si no toca ninguna tecla en 600ms, explota el evento y llama a ValidarSlugEnApi
        debounceTimer?.Start();
    }

    private async void ValidarSlugEnApi(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // Llamamos a la API
            var isFree = await NegocioService.ValidarSlugDisponible(perfilNegocio.Slug);

            slugDisponible = isFree;
            validandoSlug = false;

            // Como el Timer corre en un hilo secundario, hay que avisarle a Blazor que repinte la pantalla
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            slugDisponible = false;
            validandoSlug = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task GuardarNegocio()
    {
        guardando = true;
        mensajeExito = null;
        mensajeError = null;

        try
        {
            var exito = await NegocioService.UpdateMiNegocio(perfilNegocio);

            if (exito)
            {
                mensajeExito = "¡Los datos de tu negocio se actualizaron!";
                slugOriginal = perfilNegocio.Slug; // Actualizamos el original

                // 🎯 MAGIA: Actualizamos el AppState para que cambie el Header automáticamente
                if (State.CurrentNegocio != null)
                {
                    State.CurrentNegocio.Nombre = perfilNegocio.Nombre;
                    State.CurrentNegocio.Slug = perfilNegocio.Slug;
                    State.NotifyStateChanged();
                }
            }
            else
            {
                mensajeError = "No pudimos actualizar el negocio.";
            }
        }
        catch (Exception ex)
        {
            mensajeError = $"Error: {ex.Message}";
        }
        finally
        {
            guardando = false;
        }
    }

    // Destruimos el reloj cuando el usuario se va de la página
    public void Dispose()
    {
        debounceTimer?.Dispose();
    }

    protected async Task CargarPreviewLogo(InputFileChangeEventArgs e)
    {
        var archivo = e.File;
        if (archivo != null)
        {
            // Validamos peso en el frontend
            if (archivo.Size > 2 * 1024 * 1024)
            {
                mensajeError = "La imagen no puede pesar más de 2MB.";
                return;
            }

            logoSeleccionado = archivo;
            mensajeError = null;

            // Creamos la vista previa para que el usuario la vea instantáneamente
            var format = archivo.ContentType;
            using var stream = archivo.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var base64 = Convert.ToBase64String(ms.ToArray());
            logoPreviewUrl = $"data:{format};base64,{base64}";

            // Opcional: Podés subir el logo automáticamente al elegirlo
            // o esperar a que apriete el botón "Guardar Negocio". 
            // Te recomiendo subirlo de inmediato para mejor UX:
            await EjecutarSubidaDeLogo();
        }
    }

    private async Task EjecutarSubidaDeLogo()
    {
        if (logoSeleccionado == null) return;

        subiendoLogo = true;
        try
        {
            var nuevaUrl = await NegocioService.SubirLogo(logoSeleccionado);

            if (!string.IsNullOrEmpty(nuevaUrl))
            {
                perfilNegocio.LogoUrl = nuevaUrl; // Actualizamos el DTO

                // Actualizamos el AppState para que cambie en toda la app
                if (State.CurrentNegocio != null)
                {
                    State.CurrentNegocio.LogoUrl = nuevaUrl;
                    State.NotifyStateChanged();
                }

                mensajeExito = "¡Logo actualizado correctamente!";
            }
        }
        catch (Exception)
        {
            mensajeError = "No pudimos subir el logo. Intentá de nuevo.";
        }
        finally
        {
            subiendoLogo = false;
        }
    }
}