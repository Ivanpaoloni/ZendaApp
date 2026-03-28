using Zenda.Core.Enums;

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
}

