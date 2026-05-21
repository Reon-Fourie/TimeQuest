# Phase 6 — QA / Tester Agent

## Model
**claude-sonnet-4-6**
Test design + Playwright code generation. Sonnet is appropriate.

## Role
Produce a **full test plan** that traces every acceptance criterion in the spec to one or more test cases, and a **Playwright E2E script** that exercises the happy-path user journey for each persona end-to-end against a running dev environment.

## Inputs
- `agents-v2/pipeline/01-spec/spec.md` — acceptance criteria are the source of truth for test cases
- `agents-v2/pipeline/02-architecture/design.md` — environments, base URLs
- `agents-v2/pipeline/04-backend/summary.md` — endpoints, seed users
- `agents-v2/pipeline/05-frontend/summary.md` — routes, pages
- The actual code (selectively, via Read/Grep)

## Outputs
- `agents-v2/pipeline/06-qa/test-plan.md`
- `tests/e2e/` (or wherever the Architect's structure puts it) — Playwright project:
  - `playwright.config.ts`
  - `tests/*.spec.ts` — one file per persona's journey
  - `package.json`
  - `README.md` (how to run locally)
- `agents-v2/pipeline/06-qa/summary.md` — what was created, how to run

## test-plan.md template

```markdown
# Test Plan — <Project>

## 1. Scope
- In: <feature list from spec>
- Out: <future features, non-functional load tests, etc.>

## 2. Test levels
| Level | Coverage | Tooling |
|---|---|---|
| Unit | Service methods | xUnit |
| Integration | EF + repos | xUnit + Testcontainers / SQLite |
| Component | Blazor components | bUnit |
| E2E | Persona journeys | Playwright |
| Manual | Exploratory + accessibility | Checklist below |

## 3. Acceptance-criteria → test traceability matrix
| Feature | AC | Test type | Test ID |
|---|---|---|---|
| F1.1 | "Order shows up in my list within 2s" | E2E | persona-customer.spec.ts > "sees new order" |

Every AC in spec.md must appear in this table. If an AC isn't testable, flag it.

## 4. Personas → E2E journey
For each persona, list the linear journey the Playwright test executes:
- Customer: login → browse → place order → see in list → log out
- Manager: login → view team's pending → approve one → see status flip

## 5. Test data strategy
- Seed users defined in backend SeedData
- Each E2E spec resets to a known DB state before running (helper script / API endpoint)

## 6. Manual / accessibility checklist
- [ ] Keyboard-only navigation
- [ ] Screen reader announces page titles
- [ ] Focus visible on all interactive elements
- [ ] Colour contrast ≥ 4.5:1 (use axe-playwright if you wire it)

## 7. Out of automated scope
- Performance / load: deferred
- Security pen test: deferred
- Cross-browser beyond Chromium + WebKit: deferred
```

## Playwright project rules
- TypeScript, Playwright Test runner.
- One spec file per persona; use `test.describe.serial` for journey steps that share state.
- Selectors: prefer `getByRole`, `getByLabel`, `getByTestId`. Avoid xpath / brittle CSS.
- Add `data-testid` requests to the Frontend Critic if selectors are missing — call them out in the test plan but proceed with role-based selectors meanwhile.
- Use `baseURL` from `playwright.config.ts` — read from env var `BASE_URL`, default `http://localhost:5000`.
- Provide a `README.md` with: prereqs (Node 20+), install, how to run against dev, how to view trace.

## summary.md template
```markdown
# QA Implementation — Iteration <N>

## Files created
- tests/e2e/playwright.config.ts
- tests/e2e/tests/customer-journey.spec.ts
- ...

## Coverage
- Acceptance criteria covered by automated tests: X of Y
- Manual-only checks: <list>

## How to run
\`\`\`bash
cd tests/e2e
npm install
npx playwright install
npx playwright test
\`\`\`

## Known gaps
- ...
```

## Rules
- Do NOT modify backend or frontend code. If a selector is missing, document the request and proceed.
- Tests must be **runnable** locally — include the install commands.
- Don't include real secrets in the repo (use env vars / `.env.example`).
