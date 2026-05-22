/**
 * Financial Admin journey — F2.4, F3.1, F3.2, F5.1, F5.2
 *
 * PREREQUISITE: /auth/dev-login?role=FinancialAdmin endpoint must be implemented.
 * PREREQUISITE: Seed data must include ApprovedByLead and FinancialApproved
 * timesheets for full queue/lock tests.
 */
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { loginAs } from '../fixtures/auth.fixture';

test.describe.serial('Financial Admin journey', () => {
  test('dev login as FinancialAdmin redirects to timesheet', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await expect(page).toHaveURL(/\/timesheet/);
  });

  test('nav shows Finance items for FinancialAdmin', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/queue');
    await expect(page.getByRole('link', { name: /review queue/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /lock timesheets/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /reports/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /export/i })).toBeVisible();
  });

  test('financial queue page renders', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/queue');
    await expect(page.getByRole('heading', { name: /financial approval queue/i })).toBeVisible({ timeout: 15_000 });
  });

  test('financial queue shows empty or data state', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/queue');
    await page.waitForLoadState('networkidle');
    const hasTable = await page.getByRole('table').isVisible();
    const hasEmpty = await page.getByText(/no timesheets/i).isVisible();
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('lock page renders and shows queue', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/lock');
    await expect(page.getByRole('heading', { name: /lock timesheets/i })).toBeVisible({ timeout: 15_000 });
    await page.waitForLoadState('networkidle');
    const hasTable = await page.getByRole('table').isVisible();
    const hasEmpty = await page.getByText(/no timesheets/i).isVisible();
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('reports page renders with filter form', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/reports');
    await expect(page.getByRole('heading', { name: /reconciliation reports/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByLabel(/report type/i)).toBeVisible();
    await expect(page.getByLabel(/from/i)).toBeVisible();
    await expect(page.getByLabel(/to/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /generate report/i })).toBeVisible();
  });

  test('reports form validates date range', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/reports');
    await page.waitForLoadState('networkidle');
    // Set from > to to trigger cross-field validation
    const fromInput = page.getByLabel(/from/i);
    const toInput = page.getByLabel(/to/i);
    await fromInput.fill('2026-05-31');
    await toInput.fill('2026-05-01');
    await page.getByRole('button', { name: /generate report/i }).click();
    await expect(page.getByText(/from date must be on or before/i)).toBeVisible();
  });

  test('export page renders with filter form', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/export');
    await expect(page.getByRole('heading', { name: /export locked timesheets/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByLabel(/date from/i)).toBeVisible();
    await expect(page.getByLabel(/date to/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /preview/i })).toBeVisible();
  });

  test('audit log page renders with search form', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/audit');
    await expect(page.getByRole('heading', { name: /audit log/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByLabel(/action type/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /search/i })).toBeVisible();
  });

  test('audit search returns results or empty state', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/audit');
    await page.getByRole('button', { name: /search/i }).click();
    await page.waitForLoadState('networkidle');
    const hasTable = await page.getByRole('table').isVisible();
    const hasEmpty = await page.getByText(/no audit entries/i).isVisible();
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('FinancialAdmin cannot access /admin/teams (403)', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/admin/teams');
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  test('a11y: no serious violations on reports page', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/financial/reports');
    await page.waitForLoadState('networkidle');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });

  test('a11y: no serious violations on audit log page', async ({ page }) => {
    await loginAs(page, 'FinancialAdmin');
    await page.goto('/audit');
    await page.waitForLoadState('networkidle');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });
});
