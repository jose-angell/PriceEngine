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

        if (request.UnitPrice <= 0)
        {
            errors["unitPrice"] = ["UnitPrice must be greater than 0."];
        }

        if (request.Quantity <= 0)
        {
            errors["quantity"] = ["Quantity must be an integer greater than 0."];
        }

        if (request.DiscountPercentage < 0 || request.DiscountPercentage > QuotationRules.MaxDiscountPercentage)
        {
            errors["discountPercentage"] =
                [$"DiscountPercentage must be an integer between 0 and {QuotationRules.MaxDiscountPercentage}."];
        }

        return errors;
    }
}
