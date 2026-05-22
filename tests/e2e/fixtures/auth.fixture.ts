import { Page } from '@playwright/test';

export type AppRole = 'TeamMember' | 'TeamLead' | 'FinancialAdmin' | 'Administrator' | 'SystemAdmin';

/**
 * Dev-only auth bypass. Calls /auth/dev-login?role=<roleName> which sets the
 * ASP.NET Core auth cookie for a seeded test user.
 *
 * IMPORTANT: This endpoint MUST only be registered in the Development environment.
 * It MUST NOT exist in production.
 */
export async function loginAs(page: Page, role: AppRole): Promise<void> {
  await page.goto(`/auth/dev-login?role=${role}`);
  // The endpoint redirects to /timesheet on success; wait for navigation.
  await page.waitForURL(url => !url.pathname.includes('/auth/'));
}

export async function logout(page: Page): Promise<void> {
  await page.goto('/auth/logout');
}
