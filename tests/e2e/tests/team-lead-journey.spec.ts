/**
 * Team Lead journey — F2.1, F2.2, F2.3
 *
 * PREREQUISITE: /auth/dev-login?role=TeamLead endpoint must be implemented.
 * PREREQUISITE: Seed data must include at least one submitted timesheet in the
 * TeamLead's team for queue tests to pass beyond the empty-state check.
 */
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { loginAs } from '../fixtures/auth.fixture';

test.describe.serial('Team Lead journey', () => {
  test('dev login as TeamLead redirects to timesheet', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await expect(page).toHaveURL(/\/timesheet/);
  });

  test('nav shows Approvals link for TeamLead', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/approvals');
    await expect(page.getByRole('link', { name: /approvals/i })).toBeVisible();
  });

  test('approvals queue page renders', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/approvals');
    await expect(page.getByRole('heading', { name: /pending approvals/i })).toBeVisible({ timeout: 15_000 });
  });

  test('approvals queue shows empty state when no submitted timesheets', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/approvals');
    await page.waitForLoadState('networkidle');
    // Either a table with rows or the empty state
    const hasTable = await page.getByRole('table').isVisible();
    const hasEmpty = await page.getByText(/no timesheets/i).isVisible();
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('TeamLead cannot access /financial/queue (403)', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/financial/queue');
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  test('approval detail page renders for a submitted timesheet', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/approvals');
    await page.waitForLoadState('networkidle');
    const firstRow = page.getByRole('row').nth(1); // skip header
    if (await firstRow.isVisible()) {
      await firstRow.click();
      await expect(page).toHaveURL(/\/approvals\/\d+/);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      // Approve and Reject buttons visible when status is Submitted
      await expect(page.getByRole('button', { name: /^approve$/i })).toBeVisible({ timeout: 10_000 });
    } else {
      test.skip(); // No submitted timesheets seeded — skip detail tests
    }
  });

  test('reject form requires minimum 10 character comment', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/approvals');
    await page.waitForLoadState('networkidle');
    const firstRow = page.getByRole('row').nth(1);
    if (await firstRow.isVisible()) {
      await firstRow.click();
      await page.waitForURL(/\/approvals\/\d+/);
      const rejectBtn = page.getByRole('button', { name: /^reject$/i });
      if (await rejectBtn.isVisible()) {
        // Type fewer than 10 chars and try to submit
        await page.getByLabel(/comment/i).fill('Short');
        await rejectBtn.click();
        // Validation message should appear
        await expect(page.getByText(/at least 10 characters/i)).toBeVisible();
      }
    } else {
      test.skip();
    }
  });

  test('a11y: no serious violations on approvals queue', async ({ page }) => {
    await loginAs(page, 'TeamLead');
    await page.goto('/approvals');
    await page.waitForLoadState('networkidle');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });
});
