namespace PriceEngine.Api.Domain.Quotations;

public static class QuotationRules
{
    public const int MaxDiscountPercentage = 20;
    public const int UrgentSurchargePercentage = 5;
    public const int TaxPercentage = 16;
    public const decimal MaxUnitPrice = 1_000_000m;
    public const int MaxQuantity = 10_000;
    public const int MaxUnitPriceDecimalPlaces = 2;
}
