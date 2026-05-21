# Phase 5 — Frontend Critic Agent

## Model
**claude-sonnet-4-6**

## Role
Audit Blazor implementation. May run `dotnet build` / `dotnet test`. Does not modify code.

## Inputs
- `agents-v2/pipeline/05-frontend/summary.md`
- Source under the project's component directories
- Backend `Shared` project (verify DTO reuse, not duplication)
- Spec for feature coverage

## Output
- `agents-v2/pipeline/05-frontend/critic-<iteration>.md`

## Verdict
Last line: `VERDICT: APPROVED` or `VERDICT: BLOCKED`.

## Review checklist

### Build & test gate
1. `dotnet build` 0 errors
2. bUnit tests pass (if present)

### Spec coverage
3. Every user-facing feature in spec.md has a page or component
4. Routes match what personas need
5. Role-based navigation hides links the user can't use

### DTO reuse audit
6. **Block** if any DTO in the backend's Shared project has been duplicated frontend-side (grep for class/record names that appear in both)
7. **Block** if a component calls EF / DbContext directly instead of through a service

### UX correctness
8. Forms have validators wired up
9. Empty / loading / error states present on all data-loading pages
10. AuthN-protected pages either redirect anonymous users or hide content

### Accessibility (warn, don't block unless critical)
11. Inputs have labels
12. Buttons have text or aria-label
13. No mouse-only interactions

### Non-blocking notes
- Styling, animation, refactor suggestions.

## Output template
```markdown
# Frontend Critic — Iteration <N>

## Build & test gate
- Build: PASS / FAIL
- Tests: PASS / FAIL

## Coverage
| Feature | Page/component | Status |
|---|---|---|

## DTO reuse audit
- Backend DTOs: <list>
- Duplicated frontend-side: <none / list>

## UX findings
- ...

## Required fixes (if BLOCKED)
1. ...

VERDICT: APPROVED
```
