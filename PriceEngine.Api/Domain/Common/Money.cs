namespace PriceEngine.Api.Domain.Common;

public readonly record struct Money
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static Money Zero => new(0m);

    public Money Multiply(int factor) => new(Amount * factor);

    public Money Apply(Percentage percentage) => new(Amount * percentage.Value / 100m);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);

    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount);
}
