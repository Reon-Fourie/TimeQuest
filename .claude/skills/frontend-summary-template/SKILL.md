---
name: frontend-summary-template
description: The summary.md template the Frontend Developer agent (phase 6) writes at the end of each iteration. Captures components created, DTOs reused from backend (the reuse contract), routes, build status, bUnit test results, accessibility checks, known gaps. Invoke ONLY when writing summary.md.
---

# Frontend Summary Template

The Frontend Critic reads this. The DTO-reuse audit and the state-coverage statements are both gating items.

```markdown
# Frontend Implementation - Iteration <N>

## Components created/modified
- Components/Pages/Orders/Index.razor (new)
- Components/Pages/Orders/Place.razor (new)
- Components/Shared/OrderCard.razor (new)
- Components/Shared/EmptyState.razor (new)
- ...

## DTOs reused (from backend Shared project)
List every DTO consumed - the critic verifies no duplication:
- OrderDto
- OrderListItemDto
- PlaceOrderRequest

Project reference: <Project>.Web -> <Project>.Shared

## Routes
| Route | Page component | Required role | Render mode |
|---|---|---|---|
| /orders | Pages/Orders/Index | Customer | InteractiveServer (default) |
| /orders/new | Pages/Orders/Place | Customer | InteractiveServer |
| /admin/users | Pages/Admin/Users | Admin | InteractiveServer |

## Build status
\`dotnet build\` -> 0 errors

## Test results
\`dotnet test\` -> N passed (bUnit tests)

(or "no bUnit tests this iteration" if none added)

## State coverage
For every data-loading component, confirm all three states are wired:
| Component | Empty | Loading | Error |
|---|---|---|---|
| Orders/Index | yes (EmptyState) | yes (LoadingSkeleton) | yes (ErrorBanner) |

## Accessibility checks
- Labels on all form inputs: PASS
- Keyboard navigation tested: <how verified - bUnit or manual>
- ARIA live regions on async status: <where>
- Skip-to-content link: present in MainLayout
- Focus visible: confirmed via :focus-visible style

## Microcopy
Used the dictionary from UI/UX design.md §8. Any deviation:
- <key>: <reason for deviation, if any>

## Known gaps
- <component not yet implemented + reason>
- <accessibility item that needs manual verification>

## Changelog (iteration >= 2 only)
- Fix from critic-1: <what changed and where>
```

## Required content invariants
- **DTOs reused**: every DTO consumed by the frontend must be listed. The critic blocks if a DTO is duplicated frontend-side instead of referenced from `Shared`.
- **Routes table**: every route in UI/UX design.md §1 (sitemap) must appear here OR be in "Known gaps".
- **State coverage table**: every data-loading component must show all three states wired.
- **Build status**: must reflect reality. Run `dotnet build` before writing.

## Build / test invariant
The agent MUST run `dotnet build` and (if bUnit tests exist) `dotnet test` before writing summary.md. The Frontend Critic will run these too.
