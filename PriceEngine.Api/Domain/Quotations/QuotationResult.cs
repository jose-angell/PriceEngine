namespace PriceEngine.Api.Domain.Quotations;

public sealed record QuotationResult(
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int DiscountPercentage,
    decimal DiscountAmount,
    decimal AmountAfterDiscount,
    bool IsUrgent,
    int SurchargePercentage,   // 5 o 0
    decimal SurchargeAmount,
    decimal TaxableBase,
    int TaxPercentage,         // 16
    decimal TaxAmount,
    decimal Total);
