# SPEC 01 — Dominio de cotizaciones y rename del proyecto

> **Estado:** aprobado
> **Depende de:** —
> **Fecha:** 2026-11-23
> **Objetivo:** Terminar el rename a `PriceEngine.Api` y establecer el dominio de cotizaciones (validación y cálculo con desglose de descuento, recargo por urgencia e impuesto) con tests unitarios.

## Alcance

**Dentro:**

- Terminar el rename `PriceEngine/` → `PriceEngine.Api/` (slnx, csproj duplicado, referencia desde tests).
- Modelo de dominio de cotización en `PriceEngine.Api/Domain/Quotations/`.
- Value objects `Money` y `Percentage` en `PriceEngine.Api/Domain/Common/`, usados internamente por el calculador.
- Validación de las reglas de entrada (precio, cantidad, descuento, nombre del servicio).
- Cálculo en orden fijo: descuento → recargo por urgencia → impuesto.
- Tests unitarios del dominio en `PriceEngine.Tests`.

**Fuera de alcance (para specs futuras):**

- Endpoint `POST /quotations`, respuesta 400 `ValidationProblem` y tests de integración (SPEC 02).
- Eliminar la plantilla `/weatherforecast`; `Program.cs` no se toca en esta spec.
- Persistencia de cotizaciones y `GET /quotations/{id}`.
- Varios servicios (líneas) por cotización.
- Catálogo de servicios.
- Tasas configurables (impuesto, recargo, tope de descuento) por configuración o por cliente.
- Monedas, autenticación, versionado de la API.

## Reglas de negocio

1. Una cotización contiene un servicio con nombre, precio unitario y cantidad.
2. `ServiceName` es obligatorio y no puede estar vacío ni ser solo espacios.
3. `UnitPrice` debe ser mayor que 0.
4. `Quantity` debe ser un entero mayor que 0.
5. `DiscountPercentage` es un entero entre 0 y 20 inclusive. Por defecto 0.
6. Si `IsUrgent` es `true` se aplica un recargo del 5%. Por defecto `false`.
7. El impuesto es del 16%.
8. Orden de cálculo:
   - `subtotal = unitPrice × quantity`
   - `discountAmount = round(subtotal × discount% / 100)`
   - `amountAfterDiscount = subtotal − discountAmount`
   - `surchargeAmount = round(amountAfterDiscount × 5 / 100)` si es urgente, si no 0
   - `taxableBase = amountAfterDiscount + surchargeAmount`
   - `taxAmount = round(taxableBase × 16 / 100)`
   - `total = taxableBase + taxAmount`
9. `round` = 2 decimales con `MidpointRounding.AwayFromZero`, aplicado en cada paso. Todos los montos son `decimal`. El redondeo vive en `Money`, no en el calculador.

Ejemplo de referencia: precio 100, cantidad 3, descuento 10, urgente →
subtotal 300.00, descuento 30.00, tras descuento 270.00, recargo 13.50, base 283.50, impuesto 45.36, total 328.86.

## Modelo de datos

```csharp
// PriceEngine.Api/Domain/Quotations/QuotationRequest.cs
public sealed record QuotationRequest(
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    int DiscountPercentage = 0,
    bool IsUrgent = false);

// PriceEngine.Api/Domain/Quotations/QuotationResult.cs
public sealed record QuotationResult(
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int DiscountPercentage,
    decimal DiscountAmount,
    decimal AmountAfterDiscount,
    bool IsUrgent,
    int SurchargePercentage,   // 5 o 0
    decimal SurchargeAmount,
    decimal TaxableBase,
    int TaxPercentage,         // 16
    decimal TaxAmount,
    decimal Total);

// PriceEngine.Api/Domain/Quotations/QuotationRules.cs
public static class QuotationRules
{
    public const int MaxDiscountPercentage = 20;
    public const int UrgentSurchargePercentage = 5;
    public const int TaxPercentage = 16;
}
```

```csharp
// PriceEngine.Api/Domain/Common/Money.cs
public readonly record struct Money
{
    public decimal Amount { get; }              // siempre redondeado a 2 decimales (AwayFromZero)
    public Money(decimal amount);               // redondea al construir
    public static Money Zero { get; }
    public Money Multiply(int factor);
    public Money Apply(Percentage percentage);  // round(Amount × percentage / 100)
    public static Money operator +(Money a, Money b);
    public static Money operator -(Money a, Money b);
}

// PriceEngine.Api/Domain/Common/Percentage.cs
public readonly record struct Percentage
{
    public int Value { get; }                   // 0..100; fuera de rango lanza ArgumentOutOfRangeException
    public Percentage(int value);
    public static Percentage Zero { get; }
}
```

- `QuotationRequest` y `QuotationResult` son DTOs de frontera con primitivos (`decimal`, `int`). Los value objects no salen del dominio, así el JSON del endpoint futuro queda plano.
- `Money` y `Percentage` protegen invariantes propias (precisión y rango técnico). Las reglas de negocio (descuento máximo 20, precio > 0) siguen en `QuotationValidator`, que no lanza excepciones.
- `QuotationCalculator` convierte el request a `Money` y `Percentage` solo después de validar.
- `QuotationValidator.Validate(QuotationRequest)` devuelve `IDictionary<string, string[]>` con los errores por campo (vacío si es válida). No lanza excepciones.
- `QuotationCalculator.Calculate(QuotationRequest)` devuelve `QuotationResult`. Asume una solicitud ya validada.
- Las claves de error usan camelCase (`serviceName`, `unitPrice`, `quantity`, `discountPercentage`) para que el endpoint futuro las exponga tal cual.

## Plan de implementación

1. Terminar el rename: eliminar `PriceEngine.Api/PriceEngine.csproj` duplicado, actualizar `PriceEngine.slnx` para incluir `PriceEngine.Api` y `PriceEngine.Tests` (y quitar `PriceEngine/`), eliminar el proyecto viejo `PriceEngine/`, añadir `ProjectReference` de Tests a Api. Verificar: `dotnet build PriceEngine.slnx` y `dotnet test PriceEngine.Tests` pasan.
2. Crear `QuotationRequest`, `QuotationResult` y `QuotationRules` en `PriceEngine.Api/Domain/Quotations/`. Verificar: compila.
3. Implementar `Money` y `Percentage` en `PriceEngine.Api/Domain/Common/`. Tests unitarios: redondeo al construir (punto medio hacia arriba), suma, resta, `Multiply`, `Apply`, igualdad por valor, y `Percentage` fuera de 0..100 lanza excepción.
4. Implementar `QuotationValidator` con las reglas 2–5. Tests unitarios por cada regla y uno con varios errores simultáneos.
5. Implementar `QuotationCalculator` usando `Money` y `Percentage`, con el orden de las reglas 8–9. Tests: sin descuento ni urgencia, solo descuento, solo urgencia, combinado (ejemplo de referencia), descuento 0 y 20, caso de redondeo en punto medio.

## Criterios de aceptación

- [ ] `PriceEngine.slnx` lista `PriceEngine.Api` y `PriceEngine.Tests`, y no lista `PriceEngine`.
- [ ] No existe `PriceEngine.Api/PriceEngine.csproj` ni la carpeta `PriceEngine/`.
- [ ] `PriceEngine.Tests` referencia a `PriceEngine.Api`.
- [ ] `dotnet build PriceEngine.slnx` termina sin errores.
- [ ] `dotnet test PriceEngine.Tests` pasa en verde.
- [ ] `new Money(2.345m).Amount` es 2.35 y `new Money(-2.345m).Amount` es -2.35.
- [ ] `new Money(10m).Apply(new Percentage(15))` es `Money` 1.50.
- [ ] `new Percentage(101)` y `new Percentage(-1)` lanzan `ArgumentOutOfRangeException`.
- [ ] Dos `Money` con el mismo monto son iguales (`==`).
- [ ] `QuotationRequest` y `QuotationResult` no exponen `Money` ni `Percentage`.
- [ ] `QuotationCalculator.Calculate` con el ejemplo de referencia devuelve `Subtotal` 300.00, `DiscountAmount` 30.00, `SurchargeAmount` 13.50, `TaxableBase` 283.50, `TaxAmount` 45.36, `Total` 328.86.
- [ ] Sin descuento ni urgencia, `DiscountAmount` y `SurchargeAmount` son 0 y `SurchargePercentage` es 0.
- [ ] `UnitPrice` 0 o negativo produce un error en `unitPrice`.
- [ ] `Quantity` 0 o negativa produce un error en `quantity`.
- [ ] `DiscountPercentage` -1 o 21 produce un error en `discountPercentage`; 0 y 20 son aceptados.
- [ ] `ServiceName` vacío o solo espacios produce un error en `serviceName`.
- [ ] Una solicitud con precio 0, cantidad 0 y descuento 25 produce los tres errores en una sola llamada a `Validate`.
- [ ] `TaxableBase + TaxAmount == Total` y todos los montos tienen como máximo 2 decimales.

## Decisiones

- **Sí:** limitar esta spec al rename y al dominio. Decisión del usuario; el endpoint va en SPEC 02.
- **Sí:** terminar el rename aquí. Sin él los comandos `dotnet` en `PriceEngine.Api/` son ambiguos.
- **Sí:** dejar `Program.cs` y `/weatherforecast` intactos. Se reemplazan al añadir el endpoint.
- **Sí:** dominio como carpetas dentro de `PriceEngine.Api`. Coincide con los placeholders existentes; separar en otro proyecto es prematuro.
- **No:** proyecto `PriceEngine.Domain` aparte. Se puede extraer cuando haya un segundo consumidor.
- **Sí:** value objects `Money` y `Percentage`. Centralizan el redondeo y el cálculo de porcentajes, que de otro modo se repetirían en cada paso del calculador.
- **No:** value objects para `UnitPrice`, `Quantity` y `ServiceName`. Solo tienen una regla de validación cada uno; un tipo por campo sería sobreingeniería.
- **Sí:** `Money` y `Percentage` lanzan excepción ante valores técnicamente imposibles (porcentaje fuera de 0..100). Son una defensa interna: el validador ya filtra las entradas del usuario, por lo que no contradice la decisión de no usar excepciones para errores de validación.
- **No:** exponer los value objects en `QuotationRequest`/`QuotationResult`. Mantiene el contrato JSON plano y evita configurar serializadores.
- **No:** `Money` con moneda (`Currency`). Monedas están fuera de alcance.
- **Sí:** redondeo a 2 decimales en cada paso con `AwayFromZero`. El desglose siempre cuadra al centavo con el total.
- **No:** redondear solo al final. El desglose podría no sumar el total.
- **Sí:** descuento como porcentaje entero 0–20. Decisión del usuario; 12.5 es inválido.
- **Sí:** el recargo se calcula sobre el monto tras descuento, y el impuesto sobre la base tras recargo (regla 7 del enunciado).
- **Sí:** validación que devuelve todos los errores, sin excepciones. El cliente corrige todo en un solo intento.
- **No:** excepciones de dominio. Más piezas para el mismo resultado.
- **Sí:** `ServiceName` obligatorio. Identifica la cotización sin requerir catálogo.
- **Sí:** tasas como constantes en `QuotationRules`. El enunciado las fija para esta práctica.
- **No:** tasas en `appsettings.json`. Va en otra spec si se necesitan configurables.
- **Sí:** cálculo stateless, sin persistencia.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Eliminar `PriceEngine/` borra el único proyecto rastreado en git antes de que el nuevo compile | El paso 1 verifica build y tests antes de dar el paso por cerrado; el historial de git conserva el proyecto viejo. |
| Errores de redondeo con `double` | Todos los montos son `decimal`; test explícito de punto medio. |

## Lo que **no** está en esta spec

- Endpoint HTTP, validación de binding y tests de integración.
- Eliminación de `/weatherforecast`.
- Persistencia y consulta de cotizaciones.
- Varias líneas de servicio por cotización.
- Catálogo de servicios.
- Tasas configurables.
- Monedas (`Money` no lleva `Currency`), autenticación y versionado de la API.
- Value objects para precio unitario, cantidad o nombre del servicio.

Cada una de ellas, si llega, va en su propia spec.
