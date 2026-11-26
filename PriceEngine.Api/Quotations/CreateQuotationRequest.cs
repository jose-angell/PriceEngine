using System.ComponentModel.DataAnnotations;
using PriceEngine.Api.Domain.Quotations;
using PriceEngine.Api.Quotations.Validation;

namespace PriceEngine.Api.Quotations;

public sealed record CreateQuotationRequest
{
    [Required(ErrorMessage = "ServiceName is required and cannot be empty or whitespace.")]
    public string? ServiceName { get; init; }

    [Required(ErrorMessage = "UnitPrice is required.")]
    [Range(0.0, (double)decimal.MaxValue, MinimumIsExclusive = true,
        ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true,
        ErrorMessage = "UnitPrice must be greater than 0.")]
    [MaxValue((double)QuotationRules.MaxUnitPrice, ErrorMessage = "UnitPrice must not exceed 1000000.")]
    [MaxDecimalPlaces(QuotationRules.MaxUnitPriceDecimalPlaces,
        ErrorMessage = "UnitPrice must have at most 2 decimal places.")]
    public decimal? UnitPrice { get; init; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, QuotationRules.MaxQuantity, ErrorMessage = "Quantity must be an integer between 1 and 10000.")]
    public int? Quantity { get; init; }

    [Range(0, QuotationRules.MaxDiscountPercentage,
        ErrorMessage = "DiscountPercentage must be an integer between 0 and 20.")]
    public int DiscountPercentage { get; init; } = 0;

    public bool IsUrgent { get; init; } = false;

    // Solo debe llamarse tras validar: usa .Value de los nullables.
    public QuotationRequest ToDomain() =>
        new(ServiceName!, UnitPrice!.Value, Quantity!.Value, DiscountPercentage, IsUrgent);
}
