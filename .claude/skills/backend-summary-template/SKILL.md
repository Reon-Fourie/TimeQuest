---
name: backend-summary-template
description: The summary.md template the Backend Developer agent (phase 5) writes at the end of each iteration. Captures files created/modified, build status, test results, feature-coverage matrix, known gaps, and iteration changelog. Invoke ONLY when writing summary.md - not needed while implementing code.
---

# Backend Summary Template

The Backend Critic reads this file to verify the iteration's outcome. The build/test gate is mandatory.

```markdown
# Backend Implementation - Iteration <N>

## Files created/modified
- src/<Project>.Domain/Entities/Order.cs (new)
- src/<Project>.Application/Services/OrderService.cs (new)
- src/<Project>.Api/Endpoints/OrdersEndpoints.cs (new)
- src/<Project>.Shared/Dtos/OrderDto.cs (new)
- tests/<Project>.Tests/Services/OrderServiceTests.cs (new)
- ...

## Build status
\`dotnet build\` -> 0 errors, 0 warnings

(or specify failures with file:line - never write "0 errors" if it isn't true)

## Test results
\`dotnet test\` -> N passed, 0 failed, 0 skipped

(or per-project counts if multi-project)

## Feature coverage
| Feature | Service method | Endpoint | Tests |
|---|---|---|---|
| F1.1 | OrderService.PlaceOrder | POST /api/orders | OrderServiceTests.PlaceOrder_HappyPath / _InsufficientStock |

Every feature in spec.md must appear here (or be in "Known gaps" with a reason).

## DTOs added to Shared project
- OrderDto (read model)
- PlaceOrderRequest (write model)
- OrderListItemDto (list projection)

The Frontend Dev will reuse these directly.

## NFR coverage
- §5.1 Performance: <how this iteration's code honours it - e.g. "Query for list uses IX_Orders_UserId_Status; tested at 100ms p95 on 10k rows">
- §5.4 Security: <auth attributes, input validation approach, secrets via KV>
- §5.5 Auditing: <which writes log to audit table>
- §5.6 Observability: <metrics emitted, log level>

## Known gaps / deferred
- <feature not yet implemented + reason>
- <test class missing because feature is incomplete>

## Changelog (iteration >= 2 only)
- Fix from critic-1: <what changed and where>
```

## Required content invariants
- **Build status** must reflect reality. Run `dotnet build` before writing. Never write "0 errors" if there are errors.
- **Test results** must reflect reality. Run `dotnet test` before writing.
- **Feature coverage table** must list every feature in spec.md. Gaps go in "Known gaps".
- **DTOs added** lists exactly the DTO type names (Frontend Dev relies on this).
- **NFR coverage** must reference at least §5.1, §5.4, §5.5, §5.6 of the spec - the critic blocks if these are silently skipped.

## Build / test invariant
The agent MUST run `dotnet build` and `dotnet test` before writing summary.md. If either fails, write the failure state honestly and STOP - don't silently mark progress. The Backend Critic will run these too and block on mismatch.
