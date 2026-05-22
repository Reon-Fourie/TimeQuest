---
name: qa-test-plan-template
description: The test-plan.md and summary.md templates used by the QA / Tester agent (phase 7). Provides the traceability matrix structure, test-level table, persona-journey format, test data strategy, manual / accessibility checklist, and out-of-scope conventions. Invoke ONLY when writing test-plan.md or summary.md.
---

# QA Test Plan & Summary Templates

## test-plan.md template

```markdown
# Test Plan - <Project>

## 1. Scope
- **In scope**: <feature list from spec.md - bullet each feature ID>
- **Out of scope** (this iteration): <future features, load tests, security pen test, etc.>

## 2. Test levels
| Level | Coverage | Tooling | Owner |
|---|---|---|---|
| Unit | Service methods, helpers | xUnit | Backend Dev |
| Integration | EF + repos, API + DB | xUnit + SQLite / Testcontainers | Backend Dev |
| Component | Blazor components | bUnit | Frontend Dev |
| E2E | Persona journeys | Playwright | QA (this phase) |
| Manual | Exploratory + accessibility | Checklist below | Human |

## 3. Acceptance-criteria -> test traceability matrix
Every AC in spec.md MUST appear here.

| Feature | AC | Test type | Test ID | Owning project |
|---|---|---|---|---|
| F1.1 | "Order shows up in my list within 2s" | E2E | persona-customer.spec.ts > "sees new order" | tests/e2e |
| F1.1 | "Order line items sum to total" | Unit | OrderServiceTests.Place_SumsLineTotals | tests/unit |
| F1.2 | ... | ... | ... | ... |

If an AC isn't testable, flag it in §7 (back to BA).

## 4. Personas -> E2E journey
One linear journey per persona, covering their key features:
- **Customer**: login -> browse catalog -> place order -> see order in list -> log out
- **Manager**: login -> view team's pending -> approve one -> see status flip
- **Admin**: login -> manage users -> assign role -> verify role applied

Each journey is one Playwright spec file (`<persona>-journey.spec.ts`).

## 5. Test data strategy
- **Seed users**: defined in backend SeedData; each persona has a known username/password in dev env (read from `.env`/Key Vault in CI)
- **Per-test reset**: each spec resets to a known DB state via a helper endpoint (`/test/reset`) OR pre-seeded test users with deterministic IDs
- **Cleanup**: each spec cleans up its own writes (delete created orders) OR uses test users with truncate-on-start

## 6. Non-functional spot checks (per spec §5)
- **§5.1 Performance**: 1 baseline run captured; assert critical paths < target ms
- **§5.4 Auth**: confirm anonymous user redirected to login on any protected page
- **§5.7 Accessibility**: axe-playwright integration on each persona journey; zero serious/critical violations

## 7. Manual / accessibility checklist
- [ ] Keyboard-only navigation through Customer journey
- [ ] Screen reader announces page titles + form errors
- [ ] Focus visible on all interactive elements
- [ ] Contrast >= 4.5:1 body / >= 3:1 large (verified via axe)
- [ ] Modal dialogs trap focus and restore on close
- [ ] Skip-to-content link works on every page
- [ ] No critical/serious axe violations on landing page

## 8. Out of automated scope
- Performance load testing (deferred)
- Security pen test (covered by phase 8 security review)
- Cross-browser beyond Chromium + WebKit (deferred)
- Mobile native (deferred unless spec §5.8 includes it)

## 9. Open questions for the team
- <any AC that wasn't testable - back to BA>
- <any selectors that weren't reliable - request data-testid in next frontend iteration>
```

## summary.md template

```markdown
# QA Implementation - Iteration <N>

## Files created
- tests/e2e/playwright.config.ts
- tests/e2e/tests/customer-journey.spec.ts
- tests/e2e/tests/manager-journey.spec.ts
- tests/e2e/README.md
- tests/e2e/package.json
- ...

## Coverage
- Acceptance criteria covered by automated tests: X of Y
- Personas with E2E journey: X of Y
- axe-playwright wired: yes/no

## How to run

\`\`\`bash
cd tests/e2e
npm install
npx playwright install
BASE_URL=http://localhost:5000 npx playwright test
\`\`\`

For trace: `npx playwright test --trace on` then `npx playwright show-trace trace.zip`

## Known gaps
- <ACs that lack automated coverage + reason>
- <selectors that need data-testid from Frontend Dev>

## Changelog (iteration >= 2 only)
- Fix from critic-1: <what changed>
```

## Required content invariants
- **Traceability matrix** in §3: EVERY AC from spec.md appears.
- **Persona journeys** in §4: EVERY persona has at least one journey.
- **Manual / accessibility checklist** in §7: present (even if items are unticked initially).
- **Out of scope** in §8: explicit. Don't leave the reader guessing.

## QA Critic gates on
- Traceability matrix coverage (every AC mapped)
- Persona journeys (every persona has one)
- Playwright project structure (config, at least one spec, package.json, README)
- README contains concrete run instructions (not "follow Playwright docs")
- No real secrets committed (regex for password / key / connection-string patterns)
