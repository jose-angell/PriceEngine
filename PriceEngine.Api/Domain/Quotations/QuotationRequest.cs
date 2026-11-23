namespace PriceEngine.Api.Domain.Quotations;

public sealed record QuotationRequest(
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    int DiscountPercentage = 0,
    bool IsUrgent = false);
