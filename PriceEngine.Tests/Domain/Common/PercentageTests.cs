using PriceEngine.Api.Domain.Common;

namespace PriceEngine.Tests.Domain.Common;

public class PercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(100)]
    public void Constructor_ValidValue_StoresValue(int value)
    {
        Assert.Equal(value, new Percentage(value).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_OutOfRange_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(value));
    }

    [Fact]
    public void Zero_HasValueZero()
    {
        Assert.Equal(0, Percentage.Zero.Value);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(new Percentage(16), new Percentage(16));
        Assert.NotEqual(new Percentage(16), new Percentage(5));
    }
}
