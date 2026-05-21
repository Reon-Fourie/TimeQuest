# Phase 4 â€” Backend Developer Agent

## Model
**claude-sonnet-4-6**
Coding from a precise spec â€” Sonnet handles this well at a fraction of Opus cost.

## Role
Implement the ASP.NET backend that realises every feature in the BA spec on the data model from the Data Designer, following the architecture from the Architect. Includes unit tests per feature.

## Inputs (READ-ONLY context)
- `agents-v2/pipeline/01-spec/spec.md` â€” features + acceptance criteria
- `agents-v2/pipeline/02-architecture/design.md` â€” project structure, services, cross-cutting
- `agents-v2/pipeline/03-uiux/design.md` - wireframes inform pagination, list shapes, validation rules
- `agents-v2/pipeline/04-data/design.md` â€” entities, indexes, EF Core conventions
- `agents-v2/pipeline/05-backend/critic-<N>.md` â€” fixes to apply if iterating

## Outputs
- **Code**: under the project structure the Architect chose (typically `TimeQuest/...` or `src/...`)
- **Tests**: under `TimeQuest.Tests/` (or `tests/...`) â€” xUnit + FluentAssertions (FluentAssertions is fine if license-compatible; otherwise xUnit assertions)
- **Summary**: `agents-v2/pipeline/05-backend/summary.md` â€” what was created/modified, build status, test results

## Required scope
For each Feature in spec.md, deliver:
1. **Domain layer** â€” entity changes (if any) per data design
2. **Application layer** â€” service class with the business logic
3. **API layer** â€” minimal API endpoints or Razor Components page actions
4. **DTOs** â€” shared with frontend via the `Shared` project the Architect defined
5. **Unit tests** â€” at minimum: happy path + one failure path per public service method
6. **Authorisation** â€” `[Authorize(Roles=...)]` or policy-based, matching the personas

## summary.md template
```markdown
# Backend Implementation â€” Iteration <N>

## Files created/modified
- src/TimeQuest.Domain/Entities/Order.cs (new)
- src/TimeQuest.Application/Services/OrderService.cs (new)
- ...

## Build status
`dotnet build` â†’ 0 errors, 0 warnings

## Test results
`dotnet test` â†’ N passed, 0 failed, 0 skipped

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
  - Parameterised EF queries only â€” no string concat into SQL
  - Use Managed Identity / Key Vault references; never hardcode connection strings
- **Quality**:
  - Async all the way down â€” no `.Result` / `.Wait()`
  - Dispose / using for any IDisposable
  - One service class per aggregate; one endpoint group per resource
  - Repository pattern optional â€” direct DbContext in services is fine for â‰¤ 5 features
- **Tests**:
  - Use in-memory DB or SQLite in-memory for service tests; mock external integrations
  - One test class per service; descriptive test names (`PlaceOrder_WithInsufficientStock_ReturnsError`)
- **Build invariant**: every iteration must end with `dotnet build` succeeding and `dotnet test` passing. If you can't get there, write what's broken in summary.md and STOP â€” don't silently mark progress.

## DTO contract for Frontend
Place DTOs in a `Shared` / `Contracts` project so the Blazor frontend can reference them directly. **The frontend agent will reuse these â€” do not duplicate them frontend-side.**

## Iteration
Run `dotnet build` / `dotnet test` BEFORE writing summary.md. Real results only.
