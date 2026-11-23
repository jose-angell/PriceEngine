using PriceEngine.Api.Domain.Quotations;

namespace PriceEngine.Api.Quotations;

public sealed record QuotationResponse(
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int DiscountPercentage,
    decimal DiscountAmount,
    decimal AmountAfterDiscount,
    bool IsUrgent,
    int SurchargePercentage,
    decimal SurchargeAmount,
    decimal TaxableBase,
    int TaxPercentage,
    decimal TaxAmount,
    decimal Total)
{
    public static QuotationResponse FromResult(QuotationResult result) => new(
        result.ServiceName,
        result.UnitPrice,
        result.Quantity,
        result.Subtotal,
        result.DiscountPercentage,
        result.DiscountAmount,
        result.AmountAfterDiscount,
        result.IsUrgent,
        result.SurchargePercentage,
        result.SurchargeAmount,
        result.TaxableBase,
        result.TaxPercentage,
        result.TaxAmount,
        result.Total);
}
