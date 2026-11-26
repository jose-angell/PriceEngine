# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

## Project state

PriceEngine is a .NET 10 (`net10.0`) ASP.NET Core minimal API that calculates service quotations (discount, urgency surcharge, tax). SPEC 01 (domain) and SPEC 02 (`POST /quotations`) are implemented. See `README.md` (in Spanish) for the business rules and the API contract; don't duplicate them here.

The solution is `PriceEngine.slnx` (requires a recent .NET SDK) and contains two projects: `PriceEngine.Api` and `PriceEngine.Tests`.

## Architecture

- `PriceEngine.Api/Domain/` is pure domain code with no ASP.NET dependencies.
  - `Common/Money` rounds to 2 decimals (`MidpointRounding.AwayFromZero`) on every construction, so every intermediate amount is rounded at each step. `Common/Percentage` is an int 0–100 and throws `ArgumentOutOfRangeException` outside that range.
  - `Quotations/QuotationRules` holds the constants (max discount 20, urgent surcharge 5, tax 16).
  - `Quotations/QuotationValidator` returns `IDictionary<string, string[]>` with camelCase keys (empty = valid).
  - `Quotations/QuotationCalculator` assumes an already-validated request. Order: subtotal → discount → urgent surcharge (on the discounted amount) → taxable base → tax → total.
- `PriceEngine.Api/Quotations/` is the HTTP boundary.
  - `CreateQuotationRequest` uses nullable properties + DataAnnotations (validated by `AddValidation()` in `Program.cs`); `ToDomain()` must only be called after validation.
  - `QuotationEndpoints.MapQuotationEndpoints()` maps `POST /quotations`, re-validates with `QuotationValidator` (defense in depth) and returns `Results<Ok<QuotationResponse>, ValidationProblem>`.
  - Error messages in the DataAnnotations and in `QuotationValidator` must stay identical.
- `Program.cs` configures camelCase dictionary keys in JSON, `AddProblemDetails()` and `AddValidation()`. The trailing `public partial class Program;` is required by `WebApplicationFactory<Program>` — don't remove it.

## Tests

`PriceEngine.Tests` (xUnit 2.9.3 + `Microsoft.AspNetCore.Mvc.Testing`) references the API project and mirrors its folder layout: unit tests for `Domain/Common` and `Domain/Quotations`, and integration tests in `Quotations/QuotationEndpointsTests.cs`. `UnitTest1.cs` is a leftover empty placeholder.

## Spec-driven workflow

New features are defined in `specs/NN-slug.md` (written in Spanish; status values like `borrador` / `implementado`) and built with the `/spec` and `/spec-impl` skills (`.claude/skills/`):
- `/spec` writes the spec as a draft; a human changes the status to approved.
- `/spec-impl NN` only works on approved specs, creates/switches to branch `spec-NN-slug`, and implements step by step pausing for review.
- Behavior is configured in `specs/.spec-config.yml` (`AutoCreateBranch: true`, `AutoCommitSteps: false`).

## Commands

```
dotnet build PriceEngine.slnx
dotnet run --project PriceEngine.Api          # http://localhost:5187
dotnet test PriceEngine.Tests
dotnet test PriceEngine.Tests --filter "FullyQualifiedName~QuotationCalculatorTests"   # subset of tests
```

`PriceEngine.Api/PriceEngine.http` holds sample requests for the running app. No linter is configured.
