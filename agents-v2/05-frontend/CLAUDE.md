# Phase 5 — Frontend Developer Agent

## Model
**claude-sonnet-4-6**

## Role
Implement the Blazor Web App that realises every user-facing feature in the BA spec, consuming the backend's services / endpoints. Reuse backend DTOs — do not duplicate them.

## Inputs
- `agents-v2/pipeline/01-spec/spec.md` — features, personas, acceptance criteria (focus on UX-visible behaviour)
- `agents-v2/pipeline/02-architecture/design.md` — Blazor render mode, project structure
- `agents-v2/pipeline/03-data/design.md` — for shape of data displayed
- `agents-v2/pipeline/04-backend/summary.md` — what services / endpoints exist, what DTOs to reuse
- The backend code itself (Read tool) — to confirm DTO names and signatures
- `agents-v2/pipeline/05-frontend/critic-<N>.md` if iterating

## Outputs
- **Code**: Blazor components under the project's `Components/` (or as the Architect specified)
- **Tests**: bUnit tests under `TimeQuest.Tests` (or a dedicated Web test project)
- **Summary**: `agents-v2/pipeline/05-frontend/summary.md`

## Required scope per persona
For each persona in `spec.md`:
1. Authenticated entry page (post-login)
2. All features that persona's user stories touch
3. Navigation: links visible only when the user has the role for the feature
4. Empty / loading / error states for any page that fetches data
5. Forms with client-side validation matching backend DataAnnotations
6. Accessible (labels for inputs, keyboard navigation, semantic headings)

## Reuse contract
- **Do not** redefine DTOs that exist in the backend's `Shared` project. Add a project reference and consume them.
- **Do not** call EF / DbContext directly from components. Always go through a service (the backend exposes one).
- For interactive-server scenarios, inject services with `@inject` and call them async.

## summary.md template
```markdown
# Frontend Implementation — Iteration <N>

## Components created
- Components/Pages/Orders/Index.razor
- Components/Pages/Orders/Place.razor
- Components/Shared/OrderCard.razor
- ...

## DTOs reused (from backend Shared project)
- OrderDto, PlaceOrderRequest, OrderListItemDto

## Routes
| Route | Page | Required role |
|---|---|---|
| /orders | OrdersIndex | Customer |

## Build status
`dotnet build` → 0 errors

## Test results
`dotnet test` → N passed (bUnit tests)

## Accessibility checks
- Labels on all form inputs: PASS
- Keyboard navigation: <how verified>

## Known gaps
- ...
```

## Rules
- **No DTO duplication.** Reference the backend's shared project.
- **No `HttpClient` to your own backend** if Interactive Server — call services directly via DI.
- Validate forms with `<DataAnnotationsValidator>` and `<ValidationSummary>`.
- Wrap async UI work with cancellation tokens scoped to the component.
- Render-mode discipline: pick `InteractiveServer` (default), `InteractiveAuto`, or `Static` per page consciously; document the choice in summary.md if not the project default.
- Empty / loading / error states are required for every data-loading component (not optional).
- Use `NavigationManager` for routing, not raw `<a href>` for internal nav.
