using PriceEngine.Api.Domain.Common;

namespace PriceEngine.Tests.Domain.Common;

public class MoneyTests
{
    [Theory]
    [InlineData(2.345, 2.35)]
    [InlineData(-2.345, -2.35)]
    [InlineData(2.344, 2.34)]
    [InlineData(2.3449, 2.34)]
    [InlineData(10, 10)]
    public void Constructor_RoundsToTwoDecimals_AwayFromZero(double input, double expected)
    {
        Assert.Equal((decimal)expected, new Money((decimal)input).Amount);
    }

    [Fact]
    public void Constructor_RoundsMidpointUp()
    {
        Assert.Equal(2.35m, new Money(2.345m).Amount);
        Assert.Equal(-2.35m, new Money(-2.345m).Amount);
    }

    [Fact]
    public void Zero_HasZeroAmount()
    {
        Assert.Equal(0m, Money.Zero.Amount);
        Assert.Equal(Money.Zero, default(Money));
    }

    [Fact]
    public void Addition_SumsAmounts()
    {
        Assert.Equal(new Money(3.75m), new Money(1.25m) + new Money(2.50m));
    }

    [Fact]
    public void Subtraction_SubtractsAmounts()
    {
        Assert.Equal(new Money(1.25m), new Money(3.75m) - new Money(2.50m));
    }

    [Fact]
    public void Multiply_MultipliesByFactor()
    {
        Assert.Equal(new Money(300m), new Money(100m).Multiply(3));
        Assert.Equal(new Money(0.30m), new Money(0.10m).Multiply(3));
    }

    [Fact]
    public void Apply_ComputesPercentage()
    {
        Assert.Equal(new Money(1.50m), new Money(10m).Apply(new Percentage(15)));
    }

    [Fact]
    public void Apply_RoundsMidpointAwayFromZero()
    {
        // 0.50 * 5 / 100 = 0.025 -> 0.03
        Assert.Equal(new Money(0.03m), new Money(0.50m).Apply(new Percentage(5)));
    }

    [Fact]
    public void Apply_ZeroPercentage_ReturnsZero()
    {
        Assert.Equal(Money.Zero, new Money(123.45m).Apply(Percentage.Zero));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.True(new Money(5m) == new Money(5.00m));
        Assert.True(new Money(2.345m) == new Money(2.35m));
        Assert.True(new Money(5m) != new Money(5.01m));
        Assert.Equal(new Money(5m).GetHashCode(), new Money(5.00m).GetHashCode());
    }
}
