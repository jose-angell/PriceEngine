using System.ComponentModel.DataAnnotations;

namespace PriceEngine.Api.Quotations.Validation;

/// <summary>Valida por valor que un decimal tenga como máximo N decimales (10.500 es válido con N = 2).
/// Null se delega a [Required].</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxDecimalPlacesAttribute(int maxDecimalPlaces) : ValidationAttribute
{
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        decimal d => decimal.Round(d, maxDecimalPlaces) == d,
        _ => true,
    };
}
