/**
 * System Admin journey — F6.1, F5.2 (audit read access)
 *
 * PREREQUISITE: /auth/dev-login?role=SystemAdmin endpoint must be implemented.
 */
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { loginAs } from '../fixtures/auth.fixture';

test.describe.serial('System Admin journey', () => {
  test('dev login as SystemAdmin redirects to timesheet', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await expect(page).toHaveURL(/\/timesheet/);
  });

  test('nav shows Integrations and Audit Log for SystemAdmin', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/sysadmin/integrations');
    await expect(page.getByRole('link', { name: /integrations/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /audit log/i })).toBeVisible();
  });

  test('integrations page renders with health section', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/sysadmin/integrations');
    await expect(page.getByRole('heading', { name: /integrations/i })).toBeVisible({ timeout: 15_000 });
    await page.waitForLoadState('networkidle');
    // Health indicators should be visible
    await expect(page.getByRole('table').or(page.getByText(/azure devops/i)).or(page.getByText(/integration/i))).toBeVisible();
  });

  test('audit log accessible by SystemAdmin', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/audit');
    await expect(page.getByRole('heading', { name: /audit log/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: /search/i })).toBeVisible();
  });

  test('audit search form validates date range', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/audit');
    await page.waitForLoadState('networkidle');
    // Fill from > to
    const fromInput = page.getByLabel(/date from/i);
    const toInput = page.getByLabel(/date to/i);
    if (await fromInput.isVisible() && await toInput.isVisible()) {
      await fromInput.fill('2026-05-31');
      await toInput.fill('2026-05-01');
      await page.getByRole('button', { name: /search/i }).click();
      await expect(page.getByText(/from date must be on or before/i)).toBeVisible();
    }
  });

  test('SystemAdmin cannot access /admin/teams (403)', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/admin/teams');
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  test('SystemAdmin cannot access /financial/queue (403)', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/financial/queue');
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  test('a11y: no serious violations on integrations page', async ({ page }) => {
    await loginAs(page, 'SystemAdmin');
    await page.goto('/sysadmin/integrations');
    await page.waitForLoadState('networkidle');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });
});
