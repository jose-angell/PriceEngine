using System.ComponentModel.DataAnnotations;

namespace PriceEngine.Api.Quotations.Validation;

/// <summary>Valida que un decimal no supere un máximo (inclusive). Null se delega a [Required].</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxValueAttribute(double maximum) : ValidationAttribute
{
    private readonly decimal _maximum = (decimal)maximum;

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        decimal d => d <= _maximum,
        _ => true,
    };
}
