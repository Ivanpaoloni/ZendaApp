namespace Zenda.Client.Services;
using Zenda.Core.DTOs;

public class AppState
{
    // 1. El negocio actual (Para mostrar el nombre/slug en el Layout sin llamar a la API siempre)
    public NegocioReadDto? CurrentNegocio { get; set; }

    // 2. El prestador que estamos configurando (El que ya tenías)
    public PrestadorReadDto? PrestadorEnEdicion { get; set; }

    // 3. La sede seleccionada (Útil si querés filtrar la agenda por sede)
    public SedeReadDto? SedeSeleccionada { get; set; }

    // --- MAGIA DE BLAZOR: Notificar cambios ---
    // Esto sirve para que si un componente cambia algo, el resto se entere
    public event Action? OnChange;

    public void NotifyStateChanged() => OnChange?.Invoke();

    // Helper para limpiar el estado al cerrar sesión
    public void Clear()
    {
        CurrentNegocio = null;
        PrestadorEnEdicion = null;
        SedeSeleccionada = null;
        NotifyStateChanged();
    }
}