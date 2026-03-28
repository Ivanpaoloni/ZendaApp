using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenda.Core.Enums
{
    public enum EstadoTurnoEnum
    {
        Pendiente,   // El cliente reservó pero no pagó/confirmó
        Confirmado,  // El turno está 100% asegurado
        Completado,  // El cliente vino y se cortó el pelo
        Cancelado,   // Se canceló (por el cliente o el negocio)
        Ausente      // El cliente no apareció (No-show)
    }
}
