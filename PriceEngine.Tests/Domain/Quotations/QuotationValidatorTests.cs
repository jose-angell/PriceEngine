using PriceEngine.Api.Domain.Quotations;

namespace PriceEngine.Tests.Domain.Quotations;

public class QuotationValidatorTests
{
    private static QuotationRequest Valid(
        string serviceName = "Consulting",
        decimal unitPrice = 100m,
        int quantity = 1,
        int discount = 0,
        bool urgent = false) => new(serviceName, unitPrice, quantity, discount, urgent);

    [Fact]
    public void ValidRequest_ReturnsNoErrors()
    {
        Assert.Empty(QuotationValidator.Validate(Valid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ServiceName_EmptyOrWhitespace_ReturnsError(string? name)
    {
        var errors = QuotationValidator.Validate(Valid(serviceName: name!));

        Assert.Single(errors);
        Assert.True(errors.ContainsKey("serviceName"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void UnitPrice_ZeroOrNegative_ReturnsError(double price)
    {
        var errors = QuotationValidator.Validate(Valid(unitPrice: (decimal)price));

        Assert.Single(errors);
        Assert.True(errors.ContainsKey("unitPrice"));
    }

    [Fact]
    public void UnitPrice_Positive_IsAccepted()
    {
        Assert.Empty(QuotationValidator.Validate(Valid(unitPrice: 0.01m)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Quantity_ZeroOrNegative_ReturnsError(int quantity)
    {
        var errors = QuotationValidator.Validate(Valid(quantity: quantity));

        Assert.Single(errors);
        Assert.True(errors.ContainsKey("quantity"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void Discount_OutOfRange_ReturnsError(int discount)
    {
        var errors = QuotationValidator.Validate(Valid(discount: discount));

        Assert.Single(errors);
        Assert.True(errors.ContainsKey("discountPercentage"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Discount_Boundaries_AreAccepted(int discount)
    {
        Assert.Empty(QuotationValidator.Validate(Valid(discount: discount)));
    }

    [Fact]
    public void MultipleInvalidFields_ReturnsAllErrorsInOneCall()
    {
        var errors = QuotationValidator.Validate(Valid(unitPrice: 0m, quantity: 0, discount: 25));

        Assert.Equal(3, errors.Count);
        Assert.True(errors.ContainsKey("unitPrice"));
        Assert.True(errors.ContainsKey("quantity"));
        Assert.True(errors.ContainsKey("discountPercentage"));
    }

    [Fact]
    public void AllFieldsInvalid_ReturnsFourErrors()
    {
        var errors = QuotationValidator.Validate(Valid(serviceName: " ", unitPrice: -1m, quantity: -1, discount: -1));

        Assert.Equal(4, errors.Count);
    }
}
