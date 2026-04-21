using System.ComponentModel.DataAnnotations.Schema;
using Zenda.Core.Models;

namespace Zenda.Core.Entities;

[Table("PlanSuscripcion")]
public class PlanSuscripcion : BaseEntity
{
    public string Nombre { get; set; } = string.Empty; // "Free", "Business", "Pro"
    public string Slug { get; set; } = string.Empty;   // "free", "business", "pro"

    // 🎯 LÍMITES CONFIGURABLES
    public int MaxSedes { get; set; }
    public int MaxProfesionales { get; set; }

    // 🎯 FEATURES BOOLEANOS
    public bool HabilitaRecordatoriosHangfire { get; set; }
    public bool HabilitaCajaAvanzada { get; set; }

    // Opcional para el futuro:
    public decimal PrecioMensual { get; set; }
    public string? MercadoPagoPlanId { get; set; }
}