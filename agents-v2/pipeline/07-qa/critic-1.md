# QA Critic — Iteration 1

## Traceability check

- **AC count in spec**: 70
- **AC mapped in test plan**: 70 — all 70 acceptance criteria appear in the §3 traceability matrix
- **Unmapped ACs**: none

## Persona coverage

| Persona | Journey spec | Status |
|---|---|---|
| Team Member | `team-member-journey.spec.ts` | PASS |
| Team Lead | `team-lead-journey.spec.ts` | PASS |
| Financial Admin | `financial-admin-journey.spec.ts` | PASS |
| Administrator | `administrator-journey.spec.ts` | PASS |
| System Admin | `system-admin-journey.spec.ts` | PASS |

All 5 personas have E2E journey specs. Each spec uses `test.describe.serial`. Each spec includes at least one `axe-playwright` accessibility assertion.

## Project structure

- `playwright.config.ts`: present — Chromium + WebKit, baseURL from env, retries in CI, traces on failure
- `package.json`: present — `@playwright/test@^1.48.0`, `@axe-core/playwright@^4.10.0`
- `README.md`: present — prerequisites (Node 20+), install instructions, run commands (local + Azure dev URL), single-spec command, report command
- `.env.example`: present — `BASE_URL` and seed user names; no real credentials
- `fixtures/auth.fixture.ts`: present — `loginAs(page, role)` helper; dev-only bypass documented
- Specs: 6 files (`smoke.spec.ts` + 5 persona journeys)

## Run instructions

README contains concrete, copy-pasteable commands. PASS.

## Manual / accessibility checklist

§7 of test-plan.md contains 14 checklist items covering keyboard navigation, screen reader, focus, contrast, modal focus trap, skip-to-content, table headers, and mobile viewport. PASS.

## Secret scan

No real passwords, tokens, or connection strings in `tests/e2e/`. `.env.example` contains only placeholder variable names (no values). `auth.fixture.ts` uses role enum strings, not credentials. PASS.

## Out-of-scope section

§8 of test-plan.md lists: load testing, pen test, Firefox/Safari mobile, bUnit, visual regression, Entra SSO full-flow. PASS.

## Findings

### Non-blocking

1. **Dev auth bypass is a hard dependency** — All 5 persona journey specs will fail at the login step until `/auth/dev-login` is built by the backend team. This is correctly documented in summary.md Known Gap #1 and test-plan §9. Not a QA-phase issue; a backend deliverable.

2. **Conditional `test.skip()` on empty seed data** — Team Lead and Financial Admin detail tests call `test.skip()` when no seed rows are present rather than failing. This is the correct approach for a first-pass spec against an empty dev DB, but these tests will be silently skipped if seed data is never added. Backend should add a workflow fixture.

3. **`getByLabel(/comment/i)` selector** — The rejection textarea label is "Comment (required to reject)". The partial regex `/comment/i` should match, but if the Approve section also has a comment-labelled element, the selector will throw an ambiguous match. Recommend adding `data-testid="reject-comment-input"` in frontend iteration 2 if this proves flaky.

4. **Smoke test auth assertion** — `smoke.spec.ts` asserts that navigating to `/timesheet` while unauthenticated either changes the URL or is redirected. Since Entra OIDC redirect is not yet wired (`GetCurrentUserId()` returns 0), the Blazor `[Authorize]` attribute may silently redirect to `/403` rather than the Entra login page. The assertion is written to accept both outcomes, which is correct.

5. **`axe-playwright` import path** — `import AxeBuilder from '@axe-core/playwright'` assumes the default export convention. If the installed version uses a named export, this requires `import { AxeBuilder }`. This is a minor dependency-version concern that will surface on first `npm install` and is trivial to fix.

## Required fixes (if BLOCKED)

None — all blocking checklist items pass.

VERDICT: APPROVED
