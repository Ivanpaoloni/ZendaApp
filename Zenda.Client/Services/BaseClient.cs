public abstract class BaseClient
{
    public string ParseError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Ocurrió un error inesperado en el servidor.";

        try
        {
            // 1. Intentamos parsear como JSON
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(raw);
            var root = jsonDoc.RootElement;

            // Caso A: El backend mandó un objeto con nuestra propiedad { "message": "..." }
            if (root.TryGetProperty("message", out var msgElement))
            {
                return msgElement.GetString() ?? "Error desconocido.";
            }

            // Caso B: Errores de validación automáticos de ASP.NET (Validation Problem Details)
            // Estructura: { "errors": { "Nombre": ["El campo es obligatorio"], ... } }
            if (root.TryGetProperty("errors", out var errorsElement))
            {
                // Tomamos el primer error de la lista para no abrumar al usuario
                var firstErrorProperty = errorsElement.EnumerateObject().FirstOrDefault();
                var firstErrorMessage = firstErrorProperty.Value.EnumerateArray().FirstOrDefault();

                return firstErrorMessage.GetString() ?? "Los datos enviados no son válidos.";
            }

            // Caso C: El backend mandó un error de tipo Title (común en .NET 8/9)
            if (root.TryGetProperty("title", out var titleElement))
            {
                return titleElement.GetString() ?? "Error en la petición.";
            }
        }
        catch
        {
            // 2. Si no es un JSON válido, asumimos que es un texto plano (String)
            // Filtramos mensajes técnicos de base de datos para no asustar al cliente
            if (raw.Contains("An error occurred while saving the entity changes") || raw.Contains("foreign key"))
            {
                return "No se pudo realizar la acción porque existen registros vinculados (profesionales o turnos).";
            }

            return raw;
        }

        return "No se pudo completar la operación.";
    }
}