namespace PriceEngine.Api.Domain.Common;

public readonly record struct Percentage
{
    public int Value { get; }

    public Percentage(int value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Percentage must be between 0 and 100.");
        }

        Value = value;
    }

    public static Percentage Zero => new(0);
}
