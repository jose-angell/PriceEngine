# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

## Project state

PriceEngine is a .NET 10 (`net10.0`) ASP.NET Core minimal API. It is at the early scaffold stage: `Program.cs` is still the default `/weatherforecast` template, and `Domain/` and `Quotations/` folders (in the old `PriceEngine/` project) are empty placeholders for the intended pricing/quotation logic. README is just the title.

## Layout (in transition)

The project is being renamed from `PriceEngine/` to `PriceEngine.Api/`:
- `PriceEngine/PriceEngine.csproj` — old project, the only one tracked in git and the only one listed in `PriceEngine.slnx`.
- `PriceEngine.Api/PriceEngine.Api.csproj` — new (untracked) copy of the Web project; still contains a stale `PriceEngine.csproj` alongside it, which would make `dotnet` commands in that folder ambiguous.
- `PriceEngine.Tests/` — new (untracked) xUnit test project (xunit 2.9.3, `UnitTest1.cs` placeholder). It has no project reference to the API yet, and is not in the `.slnx`.

When finishing the rename, update `PriceEngine.slnx` to include `PriceEngine.Api` and `PriceEngine.Tests`, remove the duplicate csproj, and add a `ProjectReference` from tests to the API.

## Commands

The solution file is the `.slnx` format (requires a recent .NET SDK).

```
dotnet build PriceEngine.slnx
dotnet run --project PriceEngine.Api          # or PriceEngine for the old project
dotnet test PriceEngine.Tests
dotnet test PriceEngine.Tests --filter "FullyQualifiedName~UnitTest1.Test1"   # single test
```

`PriceEngine.http` in the API project holds sample requests for the running app. No linter is configured.
