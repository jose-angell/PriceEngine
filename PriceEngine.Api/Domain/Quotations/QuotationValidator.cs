namespace PriceEngine.Api.Domain.Quotations;

public static class QuotationValidator
{
    public static IDictionary<string, string[]> Validate(QuotationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            errors["serviceName"] = ["ServiceName is required and cannot be empty or whitespace."];
        }

        var unitPriceErrors = new List<string>();

        if (request.UnitPrice <= 0)
        {
            unitPriceErrors.Add("UnitPrice must be greater than 0.");
        }

        if (request.UnitPrice > QuotationRules.MaxUnitPrice)
        {
            unitPriceErrors.Add($"UnitPrice must not exceed {QuotationRules.MaxUnitPrice:0}.");
        }

        if (decimal.Round(request.UnitPrice, QuotationRules.MaxUnitPriceDecimalPlaces) != request.UnitPrice)
        {
            unitPriceErrors.Add(
                $"UnitPrice must have at most {QuotationRules.MaxUnitPriceDecimalPlaces} decimal places.");
        }

        if (unitPriceErrors.Count > 0)
        {
            errors["unitPrice"] = [.. unitPriceErrors];
        }

        if (request.Quantity < 1 || request.Quantity > QuotationRules.MaxQuantity)
        {
            errors["quantity"] = [$"Quantity must be an integer between 1 and {QuotationRules.MaxQuantity}."];
        }

        if (request.DiscountPercentage < 0 || request.DiscountPercentage > QuotationRules.MaxDiscountPercentage)
        {
            errors["discountPercentage"] =
                [$"DiscountPercentage must be an integer between 0 and {QuotationRules.MaxDiscountPercentage}."];
        }

        return errors;
    }
}
