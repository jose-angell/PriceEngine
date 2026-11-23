# SPEC 02 — Endpoint POST /quotations

> **Estado:** implementado
> **Depende de:** SPEC 01
> **Fecha:** 2026-11-23
> **Objetivo:** Exponer `POST /quotations`, con DTOs validados por DataAnnotations en `PriceEngine.Api/Quotations/`, que devuelve el desglose de la cotización o un 400 `ValidationProblem`, cubierto por tests de integración.

## Alcance

**Dentro:**

- Carpeta raíz `PriceEngine.Api/Quotations/` con el endpoint y los DTOs de la API.
- DTO de entrada `CreateQuotationRequest` validado con DataAnnotations mediante la validación integrada de minimal APIs de .NET 10 (`builder.Services.AddValidation()`).
- DTO de salida `QuotationResponse`, mapeado desde `QuotationResult`.
- Endpoint `POST /quotations` registrado vía `app.MapQuotationEndpoints()`.
- Llamada defensiva a `QuotationValidator` dentro del endpoint antes de calcular.
- Eliminar la plantilla `/weatherforecast` y el record `WeatherForecast` de `Program.cs`.
- Actualizar `PriceEngine.http` con ejemplos de `POST /quotations` (válido e inválido).
- Tests de integración con `WebApplicationFactory<Program>` en `PriceEngine.Tests`.

**Fuera de alcance (para specs futuras):**

- OpenAPI / Swagger (`AddOpenApi`, `MapOpenApi`).
- Persistencia de cotizaciones, `GET /quotations/{id}` y respuesta 201 con `Location`.
- Varias líneas de servicio por cotización y catálogo de servicios.
- Tasas configurables, monedas, autenticación y versionado de la API.
- Mensajes de error localizados (todos en inglés).
- Cambios en el dominio de SPEC 01 (`Domain/` no se modifica).

## Contrato HTTP

`POST /quotations` con `Content-Type: application/json`.

Request:

```json
{
  "serviceName": "Consultoría",
  "unitPrice": 100,
  "quantity": 3,
  "discountPercentage": 10,
  "isUrgent": true
}
```

- `serviceName`, `unitPrice` y `quantity` son obligatorios.
- `discountPercentage` es opcional (default 0). `isUrgent` es opcional (default `false`).

Respuestas:

- **200 OK** con `QuotationResponse` en camelCase.
- **400 Bad Request** con `ValidationProblemDetails` (`application/problem+json`); `errors` usa claves camelCase (`serviceName`, `unitPrice`, `quantity`, `discountPercentage`), las mismas que `QuotationValidator`.
- **400 Bad Request** si el cuerpo no es JSON válido (comportamiento por defecto del binding).

## Modelo de datos

```csharp
// PriceEngine.Api/Quotations/CreateQuotationRequest.cs
public sealed record CreateQuotationRequest
{
    [Required(ErrorMessage = "ServiceName is required and cannot be empty or whitespace.")]
    public string? ServiceName { get; init; }

    [Required(ErrorMessage = "UnitPrice is required.")]
    [Range(/* > 0, MinimumIsExclusive = true */ ErrorMessage = "UnitPrice must be greater than 0.")]
    public decimal? UnitPrice { get; init; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be an integer greater than 0.")]
    public int? Quantity { get; init; }

    [Range(0, QuotationRules.MaxDiscountPercentage,
        ErrorMessage = "DiscountPercentage must be an integer between 0 and 20.")]
    public int DiscountPercentage { get; init; } = 0;

    public bool IsUrgent { get; init; } = false;

    public QuotationRequest ToDomain(); // solo tras validar; usa .Value de los nullables
}

// PriceEngine.Api/Quotations/QuotationResponse.cs
public sealed record QuotationResponse(
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int DiscountPercentage,
    decimal DiscountAmount,
    decimal AmountAfterDiscount,
    bool IsUrgent,
    int SurchargePercentage,
    decimal SurchargeAmount,
    decimal TaxableBase,
    int TaxPercentage,
    decimal TaxAmount,
    decimal Total)
{
    public static QuotationResponse FromResult(QuotationResult result);
}

// PriceEngine.Api/Quotations/QuotationEndpoints.cs
public static class QuotationEndpoints
{
    public static IEndpointRouteBuilder MapQuotationEndpoints(this IEndpointRouteBuilder app);
    // POST /quotations → Results<Ok<QuotationResponse>, ValidationProblem>
}
```

- Namespace: `PriceEngine.Api.Quotations`.
- Los campos obligatorios son nullables en el DTO para que un campo ausente produzca error `required` en su clave, en vez de llegar como 0.
- `[Required]` en `string` rechaza null, vacío y solo espacios (`AllowEmptyStrings = false`).
- Los mensajes de los atributos son el mismo texto en inglés que `QuotationValidator`.
- `QuotationResponse` tiene los mismos campos que `QuotationResult`, pero es un tipo de la API: el dominio puede cambiar sin romper el contrato JSON.
- `Program.cs` termina con `public partial class Program;` para que `WebApplicationFactory<Program>` lo encuentre.

## Plan de implementación

1. Crear `PriceEngine.Api/Quotations/` con `CreateQuotationRequest` (atributos y `ToDomain()`) y `QuotationResponse` (`FromResult`). Verificar: `dotnet build PriceEngine.slnx` compila.
2. Crear `QuotationEndpoints.MapQuotationEndpoints()` con `POST /quotations`: convierte con `ToDomain()`, llama `QuotationValidator.Validate`; si hay errores devuelve `TypedResults.ValidationProblem(errors)`; si no, `QuotationCalculator.Calculate` y `TypedResults.Ok(QuotationResponse.FromResult(...))`.
3. En `Program.cs`: añadir `builder.Services.AddValidation()`, llamar `app.MapQuotationEndpoints()`, eliminar `/weatherforecast` y `WeatherForecast`, añadir `public partial class Program;`. Verificar: `dotnet run --project PriceEngine.Api` arranca y `/weatherforecast` da 404.
4. Reemplazar el contenido de `PriceEngine.Api/PriceEngine.http` por una petición válida (ejemplo de referencia) y una inválida (precio 0, cantidad 0, descuento 25). Verificar manualmente con la app corriendo.
5. Añadir `Microsoft.AspNetCore.Mvc.Testing` (10.0.x) a `PriceEngine.Tests.csproj` y crear `PriceEngine.Tests/Quotations/QuotationEndpointsTests.cs` con `WebApplicationFactory<Program>`: 200 con el ejemplo de referencia, defaults de campos opcionales, 400 por cada campo, 400 con varios errores, campos obligatorios ausentes, JSON malformado. Verificar: `dotnet test PriceEngine.Tests` en verde.

## Criterios de aceptación

- [x] Existen `PriceEngine.Api/Quotations/CreateQuotationRequest.cs`, `QuotationResponse.cs` y `QuotationEndpoints.cs`, con namespace `PriceEngine.Api.Quotations`.
- [x] `CreateQuotationRequest` usa atributos de `System.ComponentModel.DataAnnotations` para todas sus reglas.
- [x] `Program.cs` llama `AddValidation()` y `MapQuotationEndpoints()`, y no contiene `weatherforecast` ni `WeatherForecast`.
- [x] `GET /weatherforecast` devuelve 404.
- [x] `POST /quotations` con el ejemplo de referencia devuelve 200 con `subtotal` 300.00, `discountAmount` 30.00, `surchargeAmount` 13.50, `taxableBase` 283.50, `taxAmount` 45.36, `total` 328.86.
- [x] Las propiedades del JSON de respuesta están en camelCase.
- [x] Un request sin `discountPercentage` ni `isUrgent` devuelve 200 con `discountPercentage` 0, `isUrgent` false y `surchargePercentage` 0.
- [x] `unitPrice` 0 devuelve 400 con error en `errors.unitPrice`.
- [x] `quantity` 0 devuelve 400 con error en `errors.quantity`.
- [x] `discountPercentage` 21 o -1 devuelve 400 con error en `errors.discountPercentage`; 0 y 20 devuelven 200.
- [x] `serviceName` `"   "` devuelve 400 con error en `errors.serviceName`.
- [x] Un body `{}` devuelve 400 con errores en `serviceName`, `unitPrice` y `quantity`.
- [x] Precio 0, cantidad 0 y descuento 25 devuelven 400 con los tres errores en una sola respuesta.
- [x] Las respuestas 400 de validación tienen `Content-Type` `application/problem+json`.
- [x] Un body que no es JSON válido devuelve 400.
- [x] `PriceEngine.http` contiene una petición `POST /quotations` válida y una inválida, y ninguna a `/weatherforecast`.
- [x] No hay cambios en `PriceEngine.Api/Domain/`.
- [x] `dotnet build PriceEngine.slnx` termina sin errores y `dotnet test PriceEngine.Tests` pasa en verde (incluidos los tests de SPEC 01).

## Decisiones

- **Sí:** endpoint y DTOs en `PriceEngine.Api/Quotations/` a nivel raíz de la API. Decisión del usuario; separa la frontera HTTP del dominio en `Domain/Quotations/`.
- **Sí:** DataAnnotations con la validación integrada de minimal APIs de .NET 10 (`AddValidation()`). Decisión del usuario; sin dependencias externas.
- **No:** FluentValidation o filtros de endpoint escritos a mano. Más piezas para lo que .NET 10 ya trae.
- **Sí:** llamar también `QuotationValidator` en el endpoint. Defensa en profundidad: el dominio no depende de que la frontera valide bien.
- **No:** eliminar `QuotationValidator`. Sigue siendo la fuente de verdad del dominio y tiene tests propios.
- **Sí:** DTO de entrada propio (`CreateQuotationRequest`) en vez de anotar `QuotationRequest`. El dominio no se ensucia con atributos de la API.
- **Sí:** campos obligatorios nullables con `[Required]`. Un campo ausente da un mensaje claro en vez de fallar como 0.
- **Sí:** mensajes en inglés idénticos a los del dominio. El cliente ve el mismo texto venga de donde venga el error.
- **Sí:** 200 OK con `QuotationResponse` propio. Sin persistencia no hay recurso creado, así que 201 no aplica.
- **No:** devolver `QuotationResult` directamente. Ataría el contrato JSON al dominio.
- **Sí:** registrar el endpoint con una extensión `MapQuotationEndpoints()`. `Program.cs` queda limpio y el endpoint vive en su carpeta.
- **Sí:** eliminar `/weatherforecast`. Estaba diferido desde SPEC 01 hasta añadir el endpoint.
- **Sí:** tests de integración con `WebApplicationFactory<Program>`. Prueban binding, validación y serialización reales.
- **No:** tests unitarios de atributos con `Validator.TryValidateObject`. Los de integración ya cubren las mismas reglas.
- **No:** OpenAPI en esta spec. Va en otra si se necesita.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| La validación integrada de .NET 10 depende de un source generator; si no se activa, los atributos se ignoran en silencio | La llamada defensiva a `QuotationValidator` sigue devolviendo 400; el test de `{}` (campos ausentes) solo pasa si DataAnnotations está activo. |
| Las claves de `errors` salen en PascalCase en vez de camelCase | Criterios de aceptación verifican claves camelCase; si falla, se ajusta la configuración de nombres JSON en el paso 3. |
| `[Range]` sobre `decimal` parsea límites según la cultura del sistema | Usar `ParseLimitsInInvariantCulture`/`ConvertValueInInvariantCulture` o límites enteros; test con `unitPrice` 0 y 0.01. |
| Reglas duplicadas entre atributos y `QuotationValidator` pueden divergir | Ambos usan `QuotationRules.MaxDiscountPercentage`; los tests de integración y de dominio cubren los mismos límites. |

## Lo que **no** está en esta spec

- OpenAPI / Swagger.
- Persistencia, `GET /quotations/{id}` y 201 Created.
- Varias líneas de servicio y catálogo.
- Tasas configurables, monedas, autenticación y versionado.
- Mensajes localizados.
- Cambios al dominio de SPEC 01.

Cada una de ellas, si llega, va en su propia spec.
