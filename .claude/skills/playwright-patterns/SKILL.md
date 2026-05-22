---
name: playwright-patterns
description: Playwright Test patterns for the QA agent (phase 7). Covers project structure, playwright.config.ts conventions, selector strategy (getByRole / getByLabel / getByTestId priority), test data approach, page-object pattern, axe-playwright integration, BASE_URL env-var pattern, README structure. Invoke when scaffolding the Playwright project or writing specs / answering critic findings.
---

# Playwright Patterns

## Project structure

```
tests/e2e/
  playwright.config.ts
  package.json
  README.md
  .env.example
  tests/
    customer-journey.spec.ts
    manager-journey.spec.ts
    admin-journey.spec.ts
    smoke.spec.ts             # smoke test for cd-dev pipeline
  pages/                      # page objects (optional, use for repeated nav patterns)
    login.page.ts
    orders.page.ts
  fixtures/                   # custom fixtures (e.g. authenticated context per role)
    auth.fixture.ts
  utils/
    reset-state.ts            # helper to reset DB to known state via test endpoint
```

## playwright.config.ts

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,             // journeys are serial within a file
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10_000,
    navigationTimeout: 30_000,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'webkit',   use: { ...devices['Desktop Safari'] } },
  ],
});
```

Rules:
- `baseURL` from env var, default localhost dev port.
- Retries only in CI to mask flakiness while debugging locally.
- Two browser projects: Chromium + WebKit. Don't add Firefox unless spec §5.8 demands.
- Traces / screenshots on failure - keep them small but useful.

## package.json

```json
{
  "name": "<project>-e2e",
  "private": true,
  "scripts": {
    "test": "playwright test",
    "test:headed": "playwright test --headed",
    "trace": "playwright show-report"
  },
  "devDependencies": {
    "@playwright/test": "^1.48.0",
    "@axe-core/playwright": "^4.10.0"
  }
}
```

Node 20+ required. Document this in README prerequisites.

## README.md

```markdown
# E2E Tests

## Prerequisites
- Node.js 20+
- Backend running locally OR dev environment URL (set `BASE_URL`)

## Install

\`\`\`bash
cd tests/e2e
npm install
npx playwright install
\`\`\`

## Run

Against local dev:
\`\`\`bash
npx playwright test
\`\`\`

Against deployed dev:
\`\`\`bash
BASE_URL=https://app-<project>-dev.azurewebsites.net npx playwright test
\`\`\`

Single spec:
\`\`\`bash
npx playwright test customer-journey
\`\`\`

Show last report:
\`\`\`bash
npx playwright show-report
\`\`\`

## Test users

Seeded by backend in dev:
- customer@example.com / Customer123!
- manager@example.com / Manager123!
- admin@example.com / Admin123!

Override in `.env` (gitignored) or `BASE_URL` for staging.

## Resetting state

Each spec resets via `/test/reset` (dev/qa only). Disabled in prod.
```

## Selector strategy (priority order)

1. **`page.getByRole('button', { name: 'Place order' })`** - best, mirrors a11y tree
2. **`page.getByLabel('Email')`** - for form inputs
3. **`page.getByText('Welcome, Alice')`** - for non-interactive text
4. **`page.getByTestId('order-list')`** - explicit test hook
5. **`page.locator('css=#some-id')`** - last resort

NEVER use xpath. NEVER use brittle CSS chains (`.row:nth-child(3) > .btn`).

If `getByRole` / `getByLabel` doesn't work because the frontend lacks accessible markup, REQUEST a `data-testid` from the Frontend Dev (note in summary.md §"Known gaps"). Use `getByTestId` as a documented fallback meanwhile - but prefer to fix the markup.

## Spec file structure (per-persona journey)

```typescript
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe.serial('Customer journey', () => {
  test('logs in', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: 'Sign in' }).click();
    await page.getByLabel('Email').fill('customer@example.com');
    await page.getByLabel('Password').fill('Customer123!');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('sees empty orders state on first visit', async ({ page }) => {
    await page.goto('/orders');
    await expect(page.getByRole('heading', { name: 'Your orders' })).toBeVisible();
    await expect(page.getByText('No orders yet')).toBeVisible();
  });

  test('places an order', async ({ page }) => {
    await page.goto('/orders/new');
    await page.getByLabel('Product').selectOption({ label: 'Widget' });
    await page.getByLabel('Quantity').fill('2');
    await page.getByRole('button', { name: 'Place order' }).click();
    await expect(page).toHaveURL(/\/orders\/\d+/);
    await expect(page.getByText('Order placed')).toBeVisible();
  });

  test('sees the new order in the list', async ({ page }) => {
    await page.goto('/orders');
    await expect(page.getByText('Widget')).toBeVisible();
  });

  test('a11y: no serious violations on /orders', async ({ page }) => {
    await page.goto('/orders');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });
});
```

Notes:
- `test.describe.serial` - journey steps share state and must run in order.
- One assertion per test where possible, but a journey step can have multiple `expect`s.
- Accessibility check per journey: zero serious / critical axe violations.

## Authenticated context fixture (optional, for non-journey tests)

```typescript
// fixtures/auth.fixture.ts
import { test as base } from '@playwright/test';

type AuthFixtures = {
  customerPage: import('@playwright/test').Page;
};

export const test = base.extend<AuthFixtures>({
  customerPage: async ({ browser }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await page.goto('/login');
    await page.getByLabel('Email').fill('customer@example.com');
    await page.getByLabel('Password').fill('Customer123!');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await use(page);
    await context.close();
  },
});

export { expect } from '@playwright/test';
```

Use sparingly - journeys are the primary structure. Fixtures are for cross-cutting smoke tests.

## Page-object pattern (optional)

Use when a screen is hit by multiple specs and you want to centralise selectors:

```typescript
// pages/orders.page.ts
import { Page, expect } from '@playwright/test';

export class OrdersPage {
  constructor(private page: Page) {}

  async goto() { await this.page.goto('/orders'); }
  async assertEmpty() { await expect(this.page.getByText('No orders yet')).toBeVisible(); }
  async clickPlaceNew() { await this.page.getByRole('link', { name: 'Place an order' }).click(); }
}
```

Don't pre-build POs for every page - introduce them when a spec gets noisy.

## Secrets

- Test credentials live in `.env` (gitignored).
- Include `.env.example` showing the variable names with placeholder values.
- In CI, secrets come from GitHub Secrets / Azure Pipeline variables - the spec reads from env vars.
- NEVER commit real passwords / tokens / connection strings. The QA Critic greps for these patterns.

## Smoke test (for cd-dev pipeline)

```typescript
// tests/smoke.spec.ts
import { test, expect } from '@playwright/test';

test('app responds and login form renders', async ({ page }) => {
  const response = await page.goto('/');
  expect(response?.ok()).toBeTruthy();
  await expect(page.getByRole('link', { name: 'Sign in' })).toBeVisible();
});
```

Single short spec - runs in CD pipeline after deploy to verify the app is alive. Doesn't need full journey.

## What NOT to do

- No `page.waitForTimeout(...)` - use `expect.poll` or `waitFor` with conditions.
- No sleeping between actions - Playwright auto-waits.
- No screenshot-comparison tests (visual regression) unless the spec demands - they're flaky and slow.
- No tests that depend on a specific order of execution across files - each file is independent.
- No tests modifying production data. Ever.
