using Zenda.Core.Enums;
namespace Zenda.Client.Extensions
{
    public static class EstadoTurnoExtensions
    {
        public static string ObtenerColorEstado(EstadoTurnoEnum estado) => estado switch
        {
            EstadoTurnoEnum.Pendiente => "bg-amber-100 text-amber-700",
            EstadoTurnoEnum.Confirmado => "bg-blue-100 text-blue-700",
            EstadoTurnoEnum.Completado => "bg-green-100 text-green-700",
            EstadoTurnoEnum.Cancelado => "bg-red-100 text-red-700",
            EstadoTurnoEnum.Ausente => "bg-slate-200 text-slate-600",
            _ => "bg-gray-100 text-gray-600"
        };
        // NUEVO MÉTODO: Exclusivo para las tarjetas del ZendyScheduler
        public static string ObtenerColorTarjetaCalendario(this EstadoTurnoEnum estado)
        {
            return estado switch
            {
                // Ámbar para llamar la atención sobre lo pendiente
                EstadoTurnoEnum.Pendiente => "bg-amber-500 text-white border-amber-600",

                // Azul vibrante para lo confirmado (transmite seguridad)
                EstadoTurnoEnum.Confirmado => "bg-blue-500 text-white border-blue-600",

                // Esmeralda/Verde para lo completado/pagado (éxito)
                EstadoTurnoEnum.Completado => "bg-emerald-500 text-white border-emerald-600",

                // Rojo puro para cancelaciones
                EstadoTurnoEnum.Cancelado => "bg-red-500 text-white border-red-600",

                // Fallback
                _ => "bg-slate-500 text-white border-slate-600"
            };
        }
    }



}