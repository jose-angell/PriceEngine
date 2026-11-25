# PriceEngine

API mínima en ASP.NET Core (.NET 10) que calcula cotizaciones de servicios aplicando descuento, recargo por urgencia e IVA.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (el archivo de solución usa el formato `.slnx`).

## Estructura

```
PriceEngine.Api/
  Domain/
    Common/          Money y Percentage (tipos de valor)
    Quotations/      QuotationCalculator, QuotationValidator, QuotationRules
  Quotations/        Endpoint POST /quotations y sus DTOs
  PriceEngine.http   Peticiones de ejemplo
PriceEngine.Tests/   Tests unitarios y de integración (xUnit + Mvc.Testing)
specs/               Especificaciones de cada funcionalidad
```

## Reglas de negocio

El cálculo se hace en este orden:

1. `subtotal = unitPrice × quantity`
2. Descuento: `discountPercentage` (0–20 %) sobre el subtotal.
3. Recargo por urgencia: 5 % sobre el monto con descuento (solo si `isUrgent`).
4. Base gravable: monto con descuento + recargo.
5. IVA: 16 % sobre la base gravable.
6. `total = base gravable + IVA`

Cada monto se redondea a 2 decimales (`MidpointRounding.AwayFromZero`) en cada paso.

## API

### `POST /quotations`

| Campo                | Tipo    | Requerido | Validación                    |
|----------------------|---------|-----------|-------------------------------|
| `serviceName`        | string  | Sí        | No vacío ni solo espacios     |
| `unitPrice`          | decimal | Sí        | Mayor que 0                   |
| `quantity`           | int     | Sí        | Entero mayor que 0            |
| `discountPercentage` | int     | No (0)    | Entero entre 0 y 20           |
| `isUrgent`           | bool    | No (false)| —                             |

Petición:

```json
{
  "serviceName": "Consultoría",
  "unitPrice": 100,
  "quantity": 3,
  "discountPercentage": 10,
  "isUrgent": true
}
```

Respuesta `200 OK`:

```json
{
  "serviceName": "Consultoría",
  "unitPrice": 100,
  "quantity": 3,
  "subtotal": 300.00,
  "discountPercentage": 10,
  "discountAmount": 30.00,
  "amountAfterDiscount": 270.00,
  "isUrgent": true,
  "surchargePercentage": 5,
  "surchargeAmount": 13.50,
  "taxableBase": 283.50,
  "taxPercentage": 16,
  "taxAmount": 45.36,
  "total": 328.86
}
```

Si algún dato no es válido responde `400 Bad Request` con un `ValidationProblem` que lista los errores por campo.

## Comandos

```
dotnet build PriceEngine.slnx
dotnet run --project PriceEngine.Api        # http://localhost:5187
dotnet test PriceEngine.Tests
dotnet test PriceEngine.Tests --filter "FullyQualifiedName~QuotationCalculatorTests"
```

`PriceEngine.Api/PriceEngine.http` contiene peticiones de ejemplo para probar la API en ejecución.

## Desarrollo por specs

Cada funcionalidad se define en `specs/` (por ejemplo, `01-calculo-de-cotizaciones.md` y `02-endpoint-de-cotizaciones.md`) y se implementa con las skills `/spec` y `/spec-impl`.

## Licencia

[MIT](LICENSE.txt)
