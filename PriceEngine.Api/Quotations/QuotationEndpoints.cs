using Microsoft.AspNetCore.Http.HttpResults;
using PriceEngine.Api.Domain.Quotations;

namespace PriceEngine.Api.Quotations;

public static class QuotationEndpoints
{
    public static IEndpointRouteBuilder MapQuotationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/quotations", CreateQuotation);
        return app;
    }

    private static Results<Ok<QuotationResponse>, ValidationProblem> CreateQuotation(CreateQuotationRequest request)
    {
        var domainRequest = request.ToDomain();

        // Defensa en profundidad: el dominio no depende de que la frontera valide bien.
        var errors = QuotationValidator.Validate(domainRequest);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = QuotationCalculator.Calculate(domainRequest);
        return TypedResults.Ok(QuotationResponse.FromResult(result));
    }
}
