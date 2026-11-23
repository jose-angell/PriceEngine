using System.Text.Json;
using PriceEngine.Api.Quotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapQuotationEndpoints();

app.Run();

public partial class Program;
