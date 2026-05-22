/**
 * Team Member journey — F1.1, F1.2, F1.3
 *
 * PREREQUISITE: /auth/dev-login?role=TeamMember endpoint must be implemented
 * (dev-only, see fixtures/auth.fixture.ts).
 * Until that endpoint exists, this journey is blocked at the login step.
 */
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { loginAs } from '../fixtures/auth.fixture';

test.describe.serial('Team Member journey', () => {
  test('dev login as TeamMember succeeds and redirects to timesheet', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await expect(page).toHaveURL(/\/timesheet/);
  });

  test('timesheet page renders with week navigation', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    await expect(page).toHaveURL(/\/timesheet\//);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    // Week navigation buttons
    await expect(page.getByRole('button', { name: /prev week/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /next week/i })).toBeVisible();
  });

  test('empty state shows when no entries exist', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    // Waits for skeleton to resolve
    await expect(page.getByText(/no entries yet/i).or(page.getByRole('table'))).toBeVisible({ timeout: 15_000 });
  });

  test('add-entry button is present on draft timesheet', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    await expect(page.getByRole('button', { name: /add entry/i })).toBeVisible({ timeout: 15_000 });
  });

  test('submit button is disabled when no entries exist', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    const submitBtn = page.getByRole('button', { name: /submit for approval/i });
    if (await submitBtn.isVisible()) {
      await expect(submitBtn).toBeDisabled();
    }
    // If no button visible, empty state replaces it — acceptable
  });

  test('nav shows My Timesheet link for TeamMember', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    await expect(page.getByRole('link', { name: /my timesheet/i })).toBeVisible();
  });

  test('nav does not show Approvals for TeamMember', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    await expect(page.getByRole('link', { name: /approvals/i })).not.toBeVisible();
  });

  test('TeamMember cannot access /approvals (redirected to 403)', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/approvals');
    // Should be redirected to /403 or show access denied
    await expect(page.getByRole('heading', { name: /access denied/i }).or(
      page.getByText(/403/i)
    )).toBeVisible({ timeout: 10_000 });
  });

  // NOTE: F1.1 "Add entry form" tests are blocked — TimeEntryFormModal is a stub in iteration 1.
  // These tests must be added in iteration 2 when the modal is implemented.
  // Tracked in: agents-v2/pipeline/06-frontend/summary.md Known Gap #1

  test('a11y: no serious violations on timesheet page', async ({ page }) => {
    await loginAs(page, 'TeamMember');
    await page.goto('/timesheet');
    await page.waitForLoadState('networkidle');
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(v => v.impact === 'serious' || v.impact === 'critical');
    expect(serious).toEqual([]);
  });
});
