using Microsoft.AspNetCore.Authorization;

namespace Zenda.Api.Middlewares
{
    public class SuscripcionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public SuscripcionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Permitir peticiones de solo lectura
            if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // 2. Omitir endpoints públicos
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            // 3. Excepción para Webhooks de Mercado Pago
            if (context.Request.Path.StartsWithSegments("/api/mercadopago/webhook", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // 4. NUEVO: Eximir rutas de administración global y usuarios SuperAdmin
            if (context.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase) ||
                context.User.IsInRole("SuperAdmin"))
            {
                await _next(context);
                return;
            }

            if (context.Request.Path.StartsWithSegments("/api/reserva", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/disponibilidad", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/calendar", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            // 5. Validar el estado de la suscripción para tenants regulares
            var claimVigente = context.User.FindFirst("SuscripcionVigente")?.Value;

            if (claimVigente != null && bool.TryParse(claimVigente, out bool isVigente) && !isVigente)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"success\": false, \"message\": \"Suscripción inactiva. No tienes permisos para crear, editar o eliminar datos. Por favor, renueva tu plan.\"}");
                return;
            }

            await _next(context);
        }
    }
}