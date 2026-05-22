# TimeQuest E2E Tests

Playwright-based E2E test suite covering all five persona journeys.

## Prerequisites

- Node.js 20 or later
- .NET 10 (TimeQuest backend running locally or deployed to dev environment)
- Playwright browsers installed (see Install)

## Install

```bash
cd tests/e2e
npm install
npx playwright install
```

## Environment

Copy `.env.example` to `.env` and adjust:

```bash
cp .env.example .env
```

The only required variable is `BASE_URL` (defaults to `http://localhost:5000`).

## Run

Against local dev (start the app first with `dotnet run --project TimeQuest`):

```bash
npx playwright test
```

Against deployed dev environment:

```bash
BASE_URL=https://app-timequest-dev.azurewebsites.net npx playwright test
```

Single spec:

```bash
npx playwright test team-member-journey
```

Headed mode (see browser):

```bash
npm run test:headed
```

Show last HTML report:

```bash
npm run trace
```

## Auth prerequisite (dev-only bypass)

The app uses Entra ID SSO in all environments. E2E tests use a **dev-only** auth bypass:
`GET /auth/dev-login?role=<roleName>` — sets the auth cookie for a seeded test user.

This endpoint is **only registered when `ASPNETCORE_ENVIRONMENT == Development`**. It must never exist in staging or production.

If you see 404 on `/auth/dev-login`, the backend hasn't yet wired the dev bypass — see Known Gaps in `agents-v2/pipeline/07-qa/summary.md`.

## Test users

Seeded by `SeedData` in development:

| Role | Display name |
|---|---|
| TeamMember | Alice Johnson |
| TeamLead | Bob Smith |
| FinancialAdmin | Deon van der Merwe |
| Administrator | Carol Nkosi |
| SystemAdmin | Eve Pillay |

## Resetting state

The tests are designed to run against fresh dev seed data. If tests fail due to leftover state, restart the app with `--seed` flag or call the `/test/reset` endpoint (dev-only, when implemented).

## Specs

| File | Persona | Features covered |
|---|---|---|
| `smoke.spec.ts` | — | App alive, login redirect |
| `team-member-journey.spec.ts` | Team Member | F1.1, F1.3 (partial) |
| `team-lead-journey.spec.ts` | Team Lead | F2.1, F2.2 (partial) |
| `financial-admin-journey.spec.ts` | Financial Admin | F2.4, F3.1, F5.1, F3.2 |
| `administrator-journey.spec.ts` | Administrator | F4.1, F4.2 |
| `system-admin-journey.spec.ts` | System Admin | F6.1 |
