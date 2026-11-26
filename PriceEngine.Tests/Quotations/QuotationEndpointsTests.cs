using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PriceEngine.Tests.Quotations;

public class QuotationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ReferenceJson = """
        {"serviceName":"Consultoría","unitPrice":100,"quantity":3,"discountPercentage":10,"isUrgent":true}
        """;

    private readonly HttpClient _client;

    public QuotationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent Json(string body) =>
        new(body, Encoding.UTF8, "application/json");

    private async Task<(HttpResponseMessage Response, JsonElement Body)> PostAsync(string body)
    {
        var response = await _client.PostAsync("/quotations", Json(body));
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        return (response, doc.RootElement.Clone());
    }

    private static string[] ErrorKeys(JsonElement body) =>
        body.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order().ToArray();

    [Fact]
    public async Task ReferenceExample_Returns200WithBreakdown()
    {
        var (response, body) = await PostAsync(ReferenceJson);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Consultoría", body.GetProperty("serviceName").GetString());
        Assert.Equal(100m, body.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(3, body.GetProperty("quantity").GetInt32());
        Assert.Equal(300.00m, body.GetProperty("subtotal").GetDecimal());
        Assert.Equal(10, body.GetProperty("discountPercentage").GetInt32());
        Assert.Equal(30.00m, body.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(270.00m, body.GetProperty("amountAfterDiscount").GetDecimal());
        Assert.True(body.GetProperty("isUrgent").GetBoolean());
        Assert.Equal(5, body.GetProperty("surchargePercentage").GetInt32());
        Assert.Equal(13.50m, body.GetProperty("surchargeAmount").GetDecimal());
        Assert.Equal(283.50m, body.GetProperty("taxableBase").GetDecimal());
        Assert.Equal(16, body.GetProperty("taxPercentage").GetInt32());
        Assert.Equal(45.36m, body.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(328.86m, body.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Response_UsesCamelCaseProperties()
    {
        var (_, body) = await PostAsync(ReferenceJson);

        var names = body.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.All(names, n => Assert.True(char.IsLower(n[0]), $"'{n}' is not camelCase"));
        Assert.Equal(14, names.Length);
    }

    [Fact]
    public async Task OptionalFieldsOmitted_UseDefaults()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":100,"quantity":1}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("discountPercentage").GetInt32());
        Assert.False(body.GetProperty("isUrgent").GetBoolean());
        Assert.Equal(0, body.GetProperty("surchargePercentage").GetInt32());
    }

    [Fact]
    public async Task SmallestValidUnitPrice_Returns200()
    {
        var (response, _) = await PostAsync("""{"serviceName":"Consulting","unitPrice":0.01,"quantity":1}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public async Task DiscountAtLimits_Returns200(int discount)
    {
        var (response, _) = await PostAsync(
            $$"""{"serviceName":"Consulting","unitPrice":100,"quantity":1,"discountPercentage":{{discount}}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnitPriceZero_Returns400OnUnitPrice()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":0,"quantity":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["unitPrice"], ErrorKeys(body));
    }

    private static string[] Messages(JsonElement body, string key) =>
        body.GetProperty("errors").GetProperty(key).EnumerateArray().Select(e => e.GetString()!).ToArray();

    [Fact]
    public async Task UnitPriceWithMoreThanTwoDecimals_Returns400WithDecimalsMessage()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":10.005,"quantity":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["unitPrice"], ErrorKeys(body));
        Assert.Equal(["UnitPrice must have at most 2 decimal places."], Messages(body, "unitPrice"));
    }

    [Fact]
    public async Task UnitPriceAboveMax_Returns400WithMaxMessage()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":1000000.01,"quantity":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["UnitPrice must not exceed 1000000."], Messages(body, "unitPrice"));
    }

    [Fact]
    public async Task UnitPriceAboveMaxWithTooManyDecimals_ReturnsBothMessages()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":1000000.005,"quantity":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ["UnitPrice must have at most 2 decimal places.", "UnitPrice must not exceed 1000000."],
            Messages(body, "unitPrice").Order().ToArray());
    }

    [Fact]
    public async Task QuantityAboveMax_Returns400WithRangeMessage()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":100,"quantity":10001}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["quantity"], ErrorKeys(body));
        Assert.Equal(["Quantity must be an integer between 1 and 10000."], Messages(body, "quantity"));
    }

    [Fact]
    public async Task UnitPriceWithTrailingZeros_Returns200()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":10.500,"quantity":1}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10.50m, body.GetProperty("subtotal").GetDecimal());
    }

    [Fact]
    public async Task MaximumValues_Returns200()
    {
        var (response, body) = await PostAsync(
            """{"serviceName":"Consulting","unitPrice":1000000,"quantity":10000,"discountPercentage":20,"isUrgent":true}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(9_744_000_000.00m, body.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task QuantityZero_Returns400OnQuantity()
    {
        var (response, body) = await PostAsync("""{"serviceName":"Consulting","unitPrice":100,"quantity":0}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["quantity"], ErrorKeys(body));
    }

    [Theory]
    [InlineData(21)]
    [InlineData(-1)]
    public async Task DiscountOutOfRange_Returns400OnDiscountPercentage(int discount)
    {
        var (response, body) = await PostAsync(
            $$"""{"serviceName":"Consulting","unitPrice":100,"quantity":1,"discountPercentage":{{discount}}}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["discountPercentage"], ErrorKeys(body));
    }

    [Fact]
    public async Task WhitespaceServiceName_Returns400OnServiceName()
    {
        var (response, body) = await PostAsync("""{"serviceName":"   ","unitPrice":100,"quantity":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["serviceName"], ErrorKeys(body));
    }

    [Fact]
    public async Task EmptyBody_Returns400ForAllRequiredFields()
    {
        var (response, body) = await PostAsync("{}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["quantity", "serviceName", "unitPrice"], ErrorKeys(body));
    }

    [Fact]
    public async Task MultipleInvalidFields_ReturnsAllErrorsInOneResponse()
    {
        var (response, body) = await PostAsync(
            """{"serviceName":"Consulting","unitPrice":0,"quantity":0,"discountPercentage":25}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["discountPercentage", "quantity", "unitPrice"], ErrorKeys(body));
    }

    [Fact]
    public async Task ValidationErrors_UseProblemJsonContentType()
    {
        var response = await _client.PostAsync("/quotations", Json("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MalformedJson_Returns400()
    {
        var response = await _client.PostAsync("/quotations", Json("{not json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WeatherForecast_Returns404()
    {
        var response = await _client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
