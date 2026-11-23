using PriceEngine.Api.Domain.Common;

namespace PriceEngine.Api.Domain.Quotations;

public static class QuotationCalculator
{
    /// <summary>Calculates a quotation. Assumes the request was already validated.</summary>
    public static QuotationResult Calculate(QuotationRequest request)
    {
        var unitPrice = new Money(request.UnitPrice);
        var discount = new Percentage(request.DiscountPercentage);
        var surcharge = request.IsUrgent
            ? new Percentage(QuotationRules.UrgentSurchargePercentage)
            : Percentage.Zero;
        var tax = new Percentage(QuotationRules.TaxPercentage);

        var subtotal = unitPrice.Multiply(request.Quantity);
        var discountAmount = subtotal.Apply(discount);
        var amountAfterDiscount = subtotal - discountAmount;
        var surchargeAmount = amountAfterDiscount.Apply(surcharge);
        var taxableBase = amountAfterDiscount + surchargeAmount;
        var taxAmount = taxableBase.Apply(tax);
        var total = taxableBase + taxAmount;

        return new QuotationResult(
            request.ServiceName,
            request.UnitPrice,
            request.Quantity,
            subtotal.Amount,
            discount.Value,
            discountAmount.Amount,
            amountAfterDiscount.Amount,
            request.IsUrgent,
            surcharge.Value,
            surchargeAmount.Amount,
            taxableBase.Amount,
            tax.Value,
            taxAmount.Amount,
            total.Amount);
    }
}
