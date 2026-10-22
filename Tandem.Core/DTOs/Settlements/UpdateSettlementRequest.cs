using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Settlements;

// Body of PATCH /api/settlements/{id}: the payer marks "I paid" (or cancels a mark made by mistake)
public class UpdateSettlementRequest
{
    [Required(ErrorMessage = "יש לציין אם התשלום בוצע")]
    public bool? IsPaid { get; init; }
}
