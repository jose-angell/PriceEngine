using System.ComponentModel.DataAnnotations;
using PriceEngine.Api.Domain.Quotations;

namespace PriceEngine.Api.Quotations;

public sealed record CreateQuotationRequest
{
    [Required(ErrorMessage = "ServiceName is required and cannot be empty or whitespace.")]
    public string? ServiceName { get; init; }

    [Required(ErrorMessage = "UnitPrice is required.")]
    [Range(0.0, (double)decimal.MaxValue, MinimumIsExclusive = true,
        ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true,
        ErrorMessage = "UnitPrice must be greater than 0.")]
    public decimal? UnitPrice { get; init; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be an integer greater than 0.")]
    public int? Quantity { get; init; }

    [Range(0, QuotationRules.MaxDiscountPercentage,
        ErrorMessage = "DiscountPercentage must be an integer between 0 and 20.")]
    public int DiscountPercentage { get; init; } = 0;

    public bool IsUrgent { get; init; } = false;

    // Solo debe llamarse tras validar: usa .Value de los nullables.
    public QuotationRequest ToDomain() =>
        new(ServiceName!, UnitPrice!.Value, Quantity!.Value, DiscountPercentage, IsUrgent);
}
