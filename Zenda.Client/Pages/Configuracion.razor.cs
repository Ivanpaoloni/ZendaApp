using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Zenda.Core.DTOs;
using Zenda.Client.Services; // 🎯 Importamos los servicios

namespace Zenda.Client.Pages;

public partial class Configuracion : ComponentBase
{
    [CascadingParameter] protected Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    // 🎯 Inyectamos el nuevo cliente
    [Inject] protected UsuarioClient UsuarioService { get; set; } = default!;

    protected string pestañaActiva = "perfil";
    protected bool cargando = true;
    protected bool guardando = false;
    protected string? mensajeExito;
    protected string? mensajeError;

    protected UsuarioUpdateDto perfilUsuario = new();
    protected string emailUsuario = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await CargarDatosPerfil();
    }

    protected void CambiarPestaña(string pestaña)
    {
        if (pestañaActiva == pestaña) return;

        pestañaActiva = pestaña;
        mensajeExito = null;
        mensajeError = null;
    }

    private async Task CargarDatosPerfil()
    {
        try
        {
            cargando = true;

            // 🎯 Llamamos a la API real
            var usuarioDb = await UsuarioService.GetMiPerfil();

            if (usuarioDb != null)
            {
                emailUsuario = usuarioDb.Email;
                perfilUsuario.Nombre = usuarioDb.Nombre;
                perfilUsuario.Apellido = usuarioDb.Apellido;
                perfilUsuario.Telefono = usuarioDb.Telefono;
            }
            else
            {
                mensajeError = "No se pudo cargar la información de tu cuenta.";
            }
        }
        catch (Exception ex)
        {
            mensajeError = "Ocurrió un error de conexión.";
            Console.WriteLine($"Error al cargar perfil: {ex.Message}");
        }
        finally
        {
            cargando = false;
        }
    }

    protected async Task GuardarPerfil()
    {
        guardando = true;
        mensajeExito = null;
        mensajeError = null;

        try
        {
            // 🎯 Enviamos los datos a la API
            var exito = await UsuarioService.UpdateMiPerfil(perfilUsuario);

            if (exito)
            {
                mensajeExito = "¡Tus datos personales se guardaron correctamente!";
            }
            else
            {
                mensajeError = "No pudimos guardar los cambios. Intentá de nuevo.";
            }
        }
        catch (Exception ex)
        {
            mensajeError = "Ocurrió un error al guardar.";
        }
        finally
        {
            guardando = false;
        }
    }

    protected string ObtenerIniciales()
    {
        var iniciales = "";
        if (!string.IsNullOrWhiteSpace(perfilUsuario.Nombre))
            iniciales += perfilUsuario.Nombre[0];

        if (!string.IsNullOrWhiteSpace(perfilUsuario.Apellido))
            iniciales += perfilUsuario.Apellido[0];

        return string.IsNullOrEmpty(iniciales) ? "U" : iniciales.ToUpper();
    }
}