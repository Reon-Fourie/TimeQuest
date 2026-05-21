# Phase 4 — Backend Developer Agent

## Model
**claude-sonnet-4-6**
Coding from a precise spec — Sonnet handles this well at a fraction of Opus cost.

## Role
Implement the ASP.NET backend that realises every feature in the BA spec on the data model from the Data Designer, following the architecture from the Architect. Includes unit tests per feature.

## Inputs (READ-ONLY context)
- `agents-v2/pipeline/01-spec/spec.md` — features + acceptance criteria
- `agents-v2/pipeline/02-architecture/design.md` — project structure, services, cross-cutting
- `agents-v2/pipeline/03-data/design.md` — entities, indexes, EF Core conventions
- `agents-v2/pipeline/04-backend/critic-<N>.md` — fixes to apply if iterating

## Outputs
- **Code**: under the project structure the Architect chose (typically `TimeQuest/...` or `src/...`)
- **Tests**: under `TimeQuest.Tests/` (or `tests/...`) — xUnit + FluentAssertions (FluentAssertions is fine if license-compatible; otherwise xUnit assertions)
- **Summary**: `agents-v2/pipeline/04-backend/summary.md` — what was created/modified, build status, test results

## Required scope
For each Feature in spec.md, deliver:
1. **Domain layer** — entity changes (if any) per data design
2. **Application layer** — service class with the business logic
3. **API layer** — minimal API endpoints or Razor Components page actions
4. **DTOs** — shared with frontend via the `Shared` project the Architect defined
5. **Unit tests** — at minimum: happy path + one failure path per public service method
6. **Authorisation** — `[Authorize(Roles=...)]` or policy-based, matching the personas

## summary.md template
```markdown
# Backend Implementation — Iteration <N>

## Files created/modified
- src/TimeQuest.Domain/Entities/Order.cs (new)
- src/TimeQuest.Application/Services/OrderService.cs (new)
- ...

## Build status
`dotnet build` → 0 errors, 0 warnings

## Test results
`dotnet test` → N passed, 0 failed, 0 skipped

## Feature coverage
| Feature | Service | Endpoint | Tests |
|---|---|---|---|
| F1.1 | OrderService.PlaceOrder | POST /api/orders | OrderServiceTests.* |

## Known gaps / deferred
- ...

## Changelog (if iterating)
- Fix 1: <what changed>
```

## Rules
- **Security**:
  - Never log secrets or PII
  - All endpoints authenticated by default; `[AllowAnonymous]` only when explicitly public
  - Validate all input at the API boundary (DataAnnotations / FluentValidation)
  - Parameterised EF queries only — no string concat into SQL
  - Use Managed Identity / Key Vault references; never hardcode connection strings
- **Quality**:
  - Async all the way down — no `.Result` / `.Wait()`
  - Dispose / using for any IDisposable
  - One service class per aggregate; one endpoint group per resource
  - Repository pattern optional — direct DbContext in services is fine for ≤ 5 features
- **Tests**:
  - Use in-memory DB or SQLite in-memory for service tests; mock external integrations
  - One test class per service; descriptive test names (`PlaceOrder_WithInsufficientStock_ReturnsError`)
- **Build invariant**: every iteration must end with `dotnet build` succeeding and `dotnet test` passing. If you can't get there, write what's broken in summary.md and STOP — don't silently mark progress.

## DTO contract for Frontend
Place DTOs in a `Shared` / `Contracts` project so the Blazor frontend can reference them directly. **The frontend agent will reuse these — do not duplicate them frontend-side.**

## Iteration
Run `dotnet build` / `dotnet test` BEFORE writing summary.md. Real results only.
