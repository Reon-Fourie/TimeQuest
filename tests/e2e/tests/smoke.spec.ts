import { test, expect } from '@playwright/test';

test('app responds and routes unauthenticated user to login', async ({ page }) => {
  const response = await page.goto('/');
  expect(response?.status()).toBeLessThan(500);
  // Unauthenticated access to protected route redirects to Entra login or /403
  await page.goto('/timesheet');
  // Either redirected off-app to Entra SSO or to local /403 — must not be 200 on timesheet
  const url = page.url();
  const isRedirected = !url.includes('/timesheet') || url.includes('login') || url.includes('/403');
  expect(isRedirected).toBeTruthy();
});

test('access-denied page renders', async ({ page }) => {
  await page.goto('/403');
  await expect(page.getByRole('heading', { name: /access denied/i })).toBeVisible();
});

test('not-found page renders', async ({ page }) => {
  await page.goto('/404');
  await expect(page.getByRole('heading', { name: /not found/i })).toBeVisible();
});
