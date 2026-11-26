using PriceEngine.Api.Domain.Quotations;

namespace PriceEngine.Tests.Domain.Quotations;

public class QuotationCalculatorTests
{
    [Fact]
    public void NoDiscountNoUrgency()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 100m, 3));

        Assert.Equal("Consulting", r.ServiceName);
        Assert.Equal(100m, r.UnitPrice);
        Assert.Equal(3, r.Quantity);
        Assert.Equal(300m, r.Subtotal);
        Assert.Equal(0, r.DiscountPercentage);
        Assert.Equal(0m, r.DiscountAmount);
        Assert.Equal(300m, r.AmountAfterDiscount);
        Assert.False(r.IsUrgent);
        Assert.Equal(0, r.SurchargePercentage);
        Assert.Equal(0m, r.SurchargeAmount);
        Assert.Equal(300m, r.TaxableBase);
        Assert.Equal(16, r.TaxPercentage);
        Assert.Equal(48m, r.TaxAmount);
        Assert.Equal(348m, r.Total);
    }

    [Fact]
    public void OnlyDiscount()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 100m, 3, 10));

        Assert.Equal(300m, r.Subtotal);
        Assert.Equal(30m, r.DiscountAmount);
        Assert.Equal(270m, r.AmountAfterDiscount);
        Assert.Equal(0m, r.SurchargeAmount);
        Assert.Equal(270m, r.TaxableBase);
        Assert.Equal(43.20m, r.TaxAmount);
        Assert.Equal(313.20m, r.Total);
    }

    [Fact]
    public void OnlyUrgency()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 100m, 3, IsUrgent: true));

        Assert.True(r.IsUrgent);
        Assert.Equal(5, r.SurchargePercentage);
        Assert.Equal(0m, r.DiscountAmount);
        Assert.Equal(15m, r.SurchargeAmount);
        Assert.Equal(315m, r.TaxableBase);
        Assert.Equal(50.40m, r.TaxAmount);
        Assert.Equal(365.40m, r.Total);
    }

    [Fact]
    public void Combined_ReferenceExample()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 100m, 3, 10, true));

        Assert.Equal(300.00m, r.Subtotal);
        Assert.Equal(10, r.DiscountPercentage);
        Assert.Equal(30.00m, r.DiscountAmount);
        Assert.Equal(270.00m, r.AmountAfterDiscount);
        Assert.Equal(5, r.SurchargePercentage);
        Assert.Equal(13.50m, r.SurchargeAmount);
        Assert.Equal(283.50m, r.TaxableBase);
        Assert.Equal(16, r.TaxPercentage);
        Assert.Equal(45.36m, r.TaxAmount);
        Assert.Equal(328.86m, r.Total);
    }

    [Fact]
    public void Discount_Zero_ProducesNoDiscount()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 100m, 3, 0));

        Assert.Equal(0m, r.DiscountAmount);
        Assert.Equal(r.Subtotal, r.AmountAfterDiscount);
    }

    [Fact]
    public void Discount_Max20()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 100m, 3, 20));

        Assert.Equal(60m, r.DiscountAmount);
        Assert.Equal(240m, r.AmountAfterDiscount);
        Assert.Equal(38.40m, r.TaxAmount);
        Assert.Equal(278.40m, r.Total);
    }

    [Fact]
    public void Rounding_Midpoint_InDiscount_GoesAwayFromZero()
    {
        // 10.50 * 5 / 100 = 0.525 -> 0.53
        var r = QuotationCalculator.Calculate(new("Consulting", 10.50m, 1, 5));

        Assert.Equal(0.53m, r.DiscountAmount);
        Assert.Equal(9.97m, r.AmountAfterDiscount);
        Assert.Equal(1.60m, r.TaxAmount); // 1.5952 -> 1.60
        Assert.Equal(11.57m, r.Total);
    }

    [Fact]
    public void Rounding_Midpoint_InSurcharge_GoesAwayFromZero()
    {
        // 0.50 * 5 / 100 = 0.025 -> 0.03
        var r = QuotationCalculator.Calculate(new("Consulting", 0.50m, 1, IsUrgent: true));

        Assert.Equal(0.03m, r.SurchargeAmount);
        Assert.Equal(0.53m, r.TaxableBase);
        Assert.Equal(0.08m, r.TaxAmount); // 0.0848 -> 0.08
        Assert.Equal(0.61m, r.Total);
    }

    private static void AssertBreakdownAddsUp(QuotationResult r)
    {
        Assert.Equal(r.AmountAfterDiscount, r.Subtotal - r.DiscountAmount);
        Assert.Equal(r.TaxableBase, r.AmountAfterDiscount + r.SurchargeAmount);
        Assert.Equal(r.Total, r.TaxableBase + r.TaxAmount);
    }

    [Fact]
    public void MinimumPrice_NoUrgency()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 0.01m, 1));

        Assert.Equal(0.01m, r.Subtotal);
        Assert.Equal(0m, r.DiscountAmount);
        Assert.Equal(0m, r.SurchargeAmount);
        Assert.Equal(0.01m, r.TaxableBase);
        Assert.Equal(0m, r.TaxAmount); // 0.0016 -> 0.00
        Assert.Equal(0.01m, r.Total);
        AssertBreakdownAddsUp(r);
    }

    [Fact]
    public void MinimumPrice_WithUrgency()
    {
        var r = QuotationCalculator.Calculate(new("Consulting", 0.01m, 1, IsUrgent: true));

        Assert.Equal(0.01m, r.Subtotal);
        Assert.Equal(0m, r.SurchargeAmount); // 0.0005 -> 0.00
        Assert.Equal(0.01m, r.TaxableBase);
        Assert.Equal(0m, r.TaxAmount);
        Assert.Equal(0.01m, r.Total);
        AssertBreakdownAddsUp(r);
    }

    [Fact]
    public void MaximumValues_WithMaxDiscountAndUrgency_DoNotOverflow()
    {
        var r = QuotationCalculator.Calculate(new(
            "Consulting", QuotationRules.MaxUnitPrice, QuotationRules.MaxQuantity,
            QuotationRules.MaxDiscountPercentage, true));

        Assert.Equal(10_000_000_000.00m, r.Subtotal);
        Assert.Equal(2_000_000_000.00m, r.DiscountAmount);
        Assert.Equal(8_000_000_000.00m, r.AmountAfterDiscount);
        Assert.Equal(400_000_000.00m, r.SurchargeAmount);
        Assert.Equal(8_400_000_000.00m, r.TaxableBase);
        Assert.Equal(1_344_000_000.00m, r.TaxAmount);
        Assert.Equal(9_744_000_000.00m, r.Total);
        AssertBreakdownAddsUp(r);
    }

    [Fact]
    public void Rounding_Midpoint_InDiscountAndSurcharge_GoesAwayFromZero()
    {
        // 1.45 * 10 / 100 = 0.145 -> 0.15; (1.45 - 0.15 = 1.30) * 5 / 100 = 0.065 -> 0.07
        // El IVA nunca cae en punto medio con 16%: 16 * centavos / 100 no puede terminar en .5.
        var r = QuotationCalculator.Calculate(new("Consulting", 1.45m, 1, 10, true));

        Assert.Equal(1.45m, r.Subtotal);
        Assert.Equal(0.15m, r.DiscountAmount);
        Assert.Equal(1.30m, r.AmountAfterDiscount);
        Assert.Equal(0.07m, r.SurchargeAmount);
        Assert.Equal(1.37m, r.TaxableBase);
        Assert.Equal(0.22m, r.TaxAmount); // 0.2192 -> 0.22
        Assert.Equal(1.59m, r.Total);
        AssertBreakdownAddsUp(r);
    }

    [Theory]
    [InlineData(100, 3, 10, true)]
    [InlineData(19.99, 7, 13, true)]
    [InlineData(0.01, 1, 0, false)]
    [InlineData(33.33, 3, 20, true)]
    public void Invariants_TotalAddsUp_AndAtMostTwoDecimals(double price, int qty, int discount, bool urgent)
    {
        var r = QuotationCalculator.Calculate(new("Consulting", (decimal)price, qty, discount, urgent));

        Assert.Equal(r.Total, r.TaxableBase + r.TaxAmount);
        Assert.Equal(r.AmountAfterDiscount, r.Subtotal - r.DiscountAmount);
        Assert.Equal(r.TaxableBase, r.AmountAfterDiscount + r.SurchargeAmount);
        foreach (var amount in new[]
                 {
                     r.Subtotal, r.DiscountAmount, r.AmountAfterDiscount,
                     r.SurchargeAmount, r.TaxableBase, r.TaxAmount, r.Total
                 })
        {
            Assert.Equal(amount, Math.Round(amount, 2, MidpointRounding.AwayFromZero));
        }
    }
}
