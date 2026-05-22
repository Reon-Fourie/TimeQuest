/**
 * Administrator journey — F4.1, F4.2
 *
 * PREREQUISITE: /auth/dev-login?role=Administrator endpoint must be implemented.
 */
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { loginAs } from '../fixtures/auth.fixture';

test.describe.serial('Administrator journey', () => {
  test('dev login as Administrator redirects to timesheet', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await expect(page).toHaveURL(/\/timesheet/);
  });

  test('nav shows Teams and Projects for Administrator', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/teams');
    await expect(page.getByRole('link', { name: /teams/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /projects/i })).toBeVisible();
  });

  test('teams list page renders', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/teams');
    await expect(page.getByRole('heading', { name: /teams/i })).toBeVisible({ timeout: 15_000 });
    await page.waitForLoadState('networkidle');
    const hasTable = await page.getByRole('table').isVisible();
    const hasEmpty = await page.getByText(/no teams/i).isVisible();
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('team detail page renders for an existing team', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/teams');
    await page.waitForLoadState('networkidle');
    const firstRow = page.getByRole('row').nth(1);
    if (await firstRow.isVisible()) {
      await firstRow.click();
      await expect(page).toHaveURL(/\/admin\/teams\/\d+/);
      await expect(page.getByLabel(/team name/i)).toBeVisible({ timeout: 10_000 });
      await expect(page.getByRole('button', { name: /save changes/i })).toBeVisible();
    } else {
      test.skip();
    }
  });

  test('team edit form validates required name', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/teams');
    await page.waitForLoadState('networkidle');
    const firstRow = page.getByRole('row').nth(1);
    if (await firstRow.isVisible()) {
      await firstRow.click();
      await page.waitForURL(/\/admin\/teams\/\d+/);
      await page.getByLabel(/team name/i).fill('');
      await page.getByRole('button', { name: /save changes/i }).click();
      await expect(page.getByText(/team name is required/i)).toBeVisible();
    } else {
      test.skip();
    }
  });

  test('projects list page renders', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/projects');
    await expect(page.getByRole('heading', { name: /projects/i })).toBeVisible({ timeout: 15_000 });
    await page.waitForLoadState('networkidle');
    const hasTable = await page.getByRole('table').isVisible();
    const hasEmpty = await page.getByText(/no projects/i).isVisible();
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('project detail page renders for an existing project', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/projects');
    await page.waitForLoadState('networkidle');
    const firstRow = page.getByRole('row').nth(1);
    if (await firstRow.isVisible()) {
      await firstRow.click();
      await expect(page).toHaveURL(/\/admin\/projects\/\d+/);
      await expect(page.getByLabel(/project name/i)).toBeVisible({ timeout: 10_000 });
      await expect(page.getByLabel(/billing type/i)).toBeVisible();
      await expect(page.getByRole('button', { name: /save changes/i })).toBeVisible();
    } else {
      test.skip();
    }
  });

  test('Administrator cannot access /approvals (403)', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/approvals');
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  test('Administrator cannot access /financial/queue (403)', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/financial/queue');
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  test('a11y: no serious violations on teams list', async ({ page }) => {
    await loginAs(page, 'Administrator');
    await page.goto('/admin/teams');
    await page.waitForLoadState('networkidle');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });
});
