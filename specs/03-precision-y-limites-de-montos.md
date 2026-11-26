# SPEC 03 — Precisión y límites de montos

> **Estado:** aprobado
> **Depende de:** SPEC 01, SPEC 02
> **Fecha:** 2026-11-25
> **Objetivo:** Rechazar con 400 los `unitPrice` con más de 2 decimales y los valores que puedan desbordar el cálculo, y cubrir con tests los casos límite de precisión y redondeo.

## Por qué existe esta spec

`Money` redondea a 2 decimales al construirse. Un `unitPrice` con más decimales se redondea antes de multiplicar, así que la información se pierde sin avisar:

- `unitPrice` 10.005 × `quantity` 3 da un subtotal de 30.03. Con el precio exacto sería 30.015, que redondea a 30.02.
- `unitPrice` 0.004 pasa la validación (> 0), pero se convierte en 0.00 y la cotización sale en 0.
- La respuesta devuelve `unitPrice` 10.005, aunque el cálculo usó 10.01.
- Un `unitPrice` muy grande multiplicado por `quantity` lanza `OverflowException` y la API responde 500.

## Alcance

**Dentro:**

- Nueva regla: `unitPrice` admite como máximo 2 decimales. Se juzga por valor, así que 10.500 es válido y 10.505 no.
- Nuevo tope: `unitPrice` ≤ 1,000,000.
- Nuevo tope: `quantity` ≤ 10,000.
- Constantes nuevas en `QuotationRules`.
- Reglas aplicadas en `QuotationValidator` y en los atributos de `CreateQuotationRequest`, con el mismo texto de error.
- Tests unitarios de casos límite en `MoneyTests`, `QuotationValidatorTests` y `QuotationCalculatorTests`.
- Tests de integración de los nuevos 400 en `QuotationEndpointsTests`.
- Actualizar `README.md` (tabla de validación) y `PriceEngine.http` (un ejemplo con más de 2 decimales).

**Fuera de alcance (para specs futuras):**

- Cambiar el redondeo en cada paso ni la regla `AwayFromZero` de `Money`.
- Aceptar precios con más de 2 decimales y calcular con precisión extendida.
- Normalizar el `unitPrice` de la respuesta: 10.500 sale tal cual.
- Topes configurables por `appsettings.json`.
- Límites sobre `serviceName` (longitud máxima).
- Monedas con otras precisiones (p. ej. JPY con 0 decimales).

## Reglas de negocio (cambios sobre SPEC 01)

3. `UnitPrice` debe ser mayor que 0, como máximo 1,000,000 y tener como máximo 2 decimales (`decimal.Round(unitPrice, 2) == unitPrice`).
4. `Quantity` debe ser un entero entre 1 y 10,000.

Con los topes, el subtotal máximo es 10,000,000,000.00. Queda muy lejos del límite de `decimal`, así que ningún paso del cálculo puede desbordar.

## Modelo de datos

```csharp
// PriceEngine.Api/Domain/Quotations/QuotationRules.cs
public static class QuotationRules
{
    public const int MaxDiscountPercentage = 20;
    public const int UrgentSurchargePercentage = 5;
    public const int TaxPercentage = 16;
    public const decimal MaxUnitPrice = 1_000_000m;      // nuevo
    public const int MaxQuantity = 10_000;               // nuevo
    public const int MaxUnitPriceDecimalPlaces = 2;      // nuevo
}
```

Mensajes de error. Son el mismo texto en `QuotationValidator` y en los atributos:

| Clave | Regla | Mensaje |
| --- | --- | --- |
| `unitPrice` | ≤ 0 | `UnitPrice must be greater than 0.` (sin cambios) |
| `unitPrice` | > 1,000,000 | `UnitPrice must not exceed 1000000.` |
| `unitPrice` | > 2 decimales | `UnitPrice must have at most 2 decimal places.` |
| `quantity` | < 1 o > 10,000 | `Quantity must be an integer between 1 and 10000.` (reemplaza al actual) |

- `QuotationValidator` sigue devolviendo `IDictionary<string, string[]>`. Si un precio rompe dos reglas a la vez, por ejemplo 1000000.005, `errors["unitPrice"]` contiene los dos mensajes.
- En `CreateQuotationRequest`, el `[Range]` de `UnitPrice` conserva solo el mínimo exclusivo > 0.
- Los nuevos atributos `MaxDecimalPlacesAttribute` y `MaxValueAttribute` (derivados de `ValidationAttribute`) van en `PriceEngine.Api/Quotations/Validation/` y cubren las otras dos reglas.
- `Quantity` usa `[Range(1, QuotationRules.MaxQuantity)]`.
- `Money` y `Percentage` no cambian.

## Plan de implementación

1. Añadir `MaxUnitPrice`, `MaxQuantity` y `MaxUnitPriceDecimalPlaces` a `QuotationRules`. Verificar: compila.
2. Extender `QuotationValidator` con el tope de precio, los decimales y el tope de cantidad. Actualizar el mensaje de `quantity`. Actualizar `QuotationValidatorTests`:
   - 0.01 válido.
   - 0.001 y 10.005 inválidos por decimales.
   - 10.500 válido.
   - 1,000,000 válido y 1,000,000.01 inválido.
   - 1000000.005 con dos mensajes.
   - `quantity` 10,000 válido y 10,001 inválido.
3. Crear `MaxDecimalPlacesAttribute` y `MaxValueAttribute` en `PriceEngine.Api/Quotations/Validation/`. Aplicarlos en `CreateQuotationRequest` junto con el nuevo `[Range]` de `Quantity`. Verificar: compila y los tests existentes siguen en verde, ajustando los que comprueben el texto viejo de `quantity`.
4. Añadir a `MoneyTests` los casos límite:
   - 0.005 → 0.01 y 0.004 → 0.00.
   - -0.005 → -0.01.
   - Resta con resultado negativo.
   - `Multiply` de 1,000,000 × 10,000 sin desbordar.
   - `Apply` con resultado en punto medio negativo.
5. Añadir a `QuotationCalculatorTests` los casos límite:
   - Precio mínimo 0.01 × 1, con y sin urgencia.
   - Máximos: 1,000,000 × 10,000 con descuento 20 y urgencia, sin excepción.
   - Al menos un caso donde descuento, recargo e IVA caen en punto medio (x.xx5).
   - Comprobar que `AmountAfterDiscount + SurchargeAmount == TaxableBase` y `TaxableBase + TaxAmount == Total`.
6. Añadir a `QuotationEndpointsTests`:
   - 400 para `unitPrice` 10.005, 1000000.01 y para `quantity` 10001, con el mensaje correcto.
   - 200 para `unitPrice` 10.500 y para los máximos.
7. Actualizar la tabla de validación de `README.md` y añadir a `PriceEngine.http` una petición con `unitPrice` 10.005.

## Criterios de aceptación

- [ ] `QuotationRules` expone `MaxUnitPrice` = 1000000, `MaxQuantity` = 10000 y `MaxUnitPriceDecimalPlaces` = 2.
- [ ] `QuotationValidator` devuelve error en `unitPrice` con el mensaje de decimales para 10.005 y 0.001.
- [ ] `QuotationValidator` acepta `unitPrice` 0.01, 10.5, 10.500 y 1000000.
- [ ] `QuotationValidator` devuelve error en `unitPrice` con el mensaje de tope para 1000000.01.
- [ ] Para `unitPrice` 1000000.005, `errors["unitPrice"]` tiene exactamente dos mensajes.
- [ ] `QuotationValidator` acepta `quantity` 10000 y rechaza 10001 y 0 con `Quantity must be an integer between 1 and 10000.`
- [ ] `POST /quotations` con `unitPrice` 10.005 devuelve 400 con `UnitPrice must have at most 2 decimal places.` en `errors.unitPrice`.
- [ ] `POST /quotations` con `unitPrice` 1000000.01 devuelve 400. Con `quantity` 10001 también devuelve 400.
- [ ] `POST /quotations` con `unitPrice` 1000000, `quantity` 10000, descuento 20 y urgente devuelve 200, no 500.
- [ ] `POST /quotations` con `unitPrice` 10.500 devuelve 200.
- [ ] `MoneyTests` cubre: 0.005 → 0.01, 0.004 → 0.00, -0.005 → -0.01, resta negativa, `Multiply` en el máximo y `Apply` con punto medio negativo.
- [ ] `QuotationCalculatorTests` incluye un caso con punto medio en descuento, recargo e IVA, y en todos los casos nuevos el desglose cuadra con `Total`.
- [ ] Los mensajes de `CreateQuotationRequest` y de `QuotationValidator` son idénticos.
- [ ] `Money` y `Percentage` no cambian.
- [ ] La tabla de validación de `README.md` refleja las nuevas reglas.
- [ ] `dotnet build PriceEngine.slnx` termina sin errores y `dotnet test PriceEngine.Tests` pasa en verde.

## Decisiones

- **Sí:** rechazar con 400 los precios con más de 2 decimales. Decisión del usuario; es explícito y evita pérdida silenciosa.
- **No:** calcular con precisión extendida. Obligaría a que `Money` admita montos sin redondear y rompería la regla de redondeo en cada paso.
- **No:** redondear el precio en la entrada. La pérdida seguiría ocurriendo, solo que sería visible.
- **Sí:** juzgar los decimales por valor (`decimal.Round(x, 2) == x`). Decisión del usuario; en JSON, 10.5 y 10.500 son el mismo número.
- **Sí:** topes de validación (1,000,000 y 10,000) para evitar el desbordamiento. Decisión del usuario; devuelve 400 en vez de 500.
- **No:** capturar `OverflowException`. Sin topes explícitos, el cliente no sabría qué valores son válidos.
- **Sí:** un mensaje por regla en `unitPrice`. Decisión del usuario; el cliente sabe exactamente qué corregir.
- **Sí:** `quantity` con un solo mensaje de rango (1–10000). Sigue el patrón de `discountPercentage`, y `[Range]` solo admite un mensaje.
- **Sí:** atributos propios pequeños en `Quotations/Validation/`. `[Range]` no permite un mensaje por límite ni comprobar decimales.
- **No:** `IValidatableObject` ni FluentValidation. La validación integrada de .NET 10 ya ejecuta atributos personalizados.
- **Sí:** tests unitarios y de integración. Decisión del usuario.
- **Sí:** las constantes de los topes viven en `QuotationRules`. Las comparten el dominio y los atributos, como `MaxDiscountPercentage`.
- **No:** modificar `Money`. Su comportamiento ya es correcto una vez que la entrada está validada.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El JSON numérico `10.005` se lee como `double` en algún punto y pierde precisión | `UnitPrice` es `decimal?` en el DTO; `System.Text.Json` lo lee directo a `decimal`; test de integración con 10.005. |
| Varios atributos sobre `UnitPrice` reportan errores en un orden o cantidad distinta al dominio | El criterio de 1000000.005 con dos mensajes se verifica también en integración. Si difiere, se documenta y se ajusta. |
| Clientes que hoy mandan `quantity` > 10,000 empiezan a recibir 400 | Es un cambio de contrato aceptado; queda documentado en el README. |
| Los tests existentes comparan el texto viejo del mensaje de `quantity` | El paso 3 incluye ajustar esos tests. |

## Lo que **no** está en esta spec

- Cambios en `Money` o en la regla de redondeo.
- Cálculo con precisión extendida.
- Normalizar `unitPrice` en la respuesta.
- Topes configurables.
- Límites de `serviceName`.
- Monedas con otras precisiones.

Cada una de ellas, si llega, va en su propia spec.
