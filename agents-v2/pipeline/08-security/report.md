# Security Review — Timesheet & Billing Workflow System

## 1. Scope reviewed

- **Backend**: `src/TimeQuest.Domain/`, `src/TimeQuest.Infrastructure/`, `src/TimeQuest.Shared/`
- **Frontend**: `TimeQuest/Components/` (all Blazor pages, shared components, layout, routes)
- **Infra**: Not present — deployment phase has not run yet
- **Pipelines**: Not present — no CI/CD pipeline files found
- **Dependencies**: `TimeQuest/TimeQuest.csproj`, `src/TimeQuest.Infrastructure/TimeQuest.Infrastructure.csproj`, `tests/e2e/package.json`
- **Configs**: `TimeQuest/appsettings.json`, `TimeQuest/appsettings.Development.json`

Compliance regime: POPIA (South Africa) + GDPR (EU) per spec §5.4. Auth: Entra ID SSO (deferred — not yet wired). Sensitive fields: employee names, hours, project names, approval comments, ticket references.

---

## 2. Findings summary

| Severity | Count |
|---|---|
| Critical | 0 |
| High | 1 |
| Medium | 2 |
| Low | 1 |
| Info | 2 |

---

## 3. Findings detail

### S-001 [High] IDOR — `GetTimesheetDetailAsync` does not check team scope

- **Category**: OWASP A01 — Broken Access Control / Insecure Direct Object Reference
- **Location**: `src/TimeQuest.Infrastructure/Services/ApprovalService.cs:83-89`
- **Description**: `GetTimesheetDetailAsync(approverId, timesheetId, ct)` fetches any timesheet by integer ID without verifying that the requesting approver is a lead for the timesheet owner's team. Any authenticated TeamLead can view any team's timesheet detail by incrementing the integer `timesheetId` in the URL (`/approvals/{id}` or `/financial/queue/{id}`).
- **Evidence**:
  ```csharp
  var timesheet = await _db.WeeklyTimesheets
      .Include(wt => wt.User)
      ...
      .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);  // no approverId scope check
  ```
- **Impact**: A TeamLead who is not assigned to a team can read the full entry detail (date, project, hours, notes, ticket refs) of any employee's timesheet by guessing sequential IDs. This violates POPIA/GDPR data minimisation and the spec §5.4 scoped visibility requirement.
- **Recommendation**: Add scope check before returning detail: `if (!await IsLeadForTimesheetAsync(approverId, timesheetId, ct)) return Result<TimesheetDetailDto>.Failure("Access denied.");` where `IsLeadForTimesheetAsync` checks `UserTeams` membership. Note: FinancialAdmins are exempt from scope restrictions per spec F4.3 AC4 — only the TeamLead path in `Approvals/Detail.razor` requires this fix.
- **Exploitable unauthenticated**: No (requires `[Authorize(Roles = "TeamLead")]`)

---

### S-002 [Medium] `GetCurrentUserId()` returns 0 across all frontend components

- **Category**: OWASP A07 — Identification and Authentication Failures
- **Location**: `TimeQuest/Components/Pages/Timesheet/Index.razor:259`, `Approvals/Index.razor:79`, `Approvals/Detail.razor:217` (and all other page components)
- **Description**: The ClaimsPrincipal-to-userId lookup is stubbed (`private int GetCurrentUserId() => 0`). All service calls that scope by user ID use `0` as the acting user. Currently the scope guards fail closed (user 0 has no teams, so approvals return empty or "Access denied"), which is safe. However, this stub MUST be replaced with a real `AuthenticationStateProvider`-based lookup before production. If replaced incorrectly (e.g. using `int.Parse` without null-checks on a missing claim), it could cause exceptions or silently assign the wrong user identity.
- **Evidence**:
  ```csharp
  // TODO: resolve from ClaimsPrincipal via AuthenticationStateProvider in full wiring
  private int GetCurrentUserId() => 0;
  ```
- **Impact**: Pre-production: all role-scoped operations fail closed (safe). Post-wiring: if the claim is extracted incorrectly, one user could act as another.
- **Recommendation**: Replace stubs with `[CascadingParameter] private Task<AuthenticationState> AuthState { get; set; }` and extract the `NameIdentifier` claim. Add a null/parse-fail guard that redirects to `/403` rather than substituting 0. Must be completed before any production deployment.
- **Exploitable unauthenticated**: No

---

### S-003 [Medium] No rate limiting configured on any route

- **Category**: OWASP A04 — Insecure Design / Denial of Service
- **Location**: `TimeQuest/Program.cs` (missing `AddRateLimiting` registration)
- **Description**: The application registers no rate-limiting middleware. With 500–2,000 concurrent users (spec §5.2) and a sustained 200 RPS target (spec §5.1), an authenticated user can flood approval or export endpoints without constraint, causing resource exhaustion on the Blazor Server circuit pool.
- **Evidence**: `Program.cs` has no call to `builder.Services.AddRateLimiting(...)` or `app.UseRateLimiter()`.
- **Impact**: Application-layer denial of service by any authenticated user. Export and report generation endpoints are particularly expensive (§5.1: export < 60s).
- **Recommendation**: Add `AddRateLimiting` with a fixed-window or sliding-window policy on the Blazor app's `MapRazorComponents` endpoint group, or use Azure App Service front-door rate limiting. Minimum: limit export/report generation to 5 calls per minute per user.
- **Exploitable unauthenticated**: No (Blazor Server requires auth)

---

### S-004 [Low] `appsettings.json` contains dev LocalDB connection string

- **Category**: OWASP A02 — Cryptographic Failures / Secret Management
- **Location**: `TimeQuest/appsettings.json:12-14`
- **Description**: The production-checked-in `appsettings.json` contains a dev LocalDB connection string using Windows integrated auth (`Trusted_Connection=True`). No password is embedded — this is not a secret leak. However, if a developer copies `appsettings.json` to production without replacing the `ConnectionStrings` section, the app will fail to connect but the config file structure reveals the database name (`TimeQuestDev`) and server type.
- **Evidence**:
  ```json
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TimeQuestDev;Trusted_Connection=True;"
  ```
- **Impact**: Low — no password exposure. DB name and server type disclosure only. Production MUST use an Azure Key Vault reference replacing this value.
- **Recommendation**: Replace the connection string value with `""` or a placeholder comment, and document in the deployment runbook that the production App Service must set this via a Key Vault reference (`@Microsoft.KeyVault(VaultName=...,SecretName=...)`).
- **Exploitable unauthenticated**: No

---

### S-005 [Info] Identity password options not explicitly configured

- **Category**: OWASP A07 — Identification and Authentication Failures
- **Location**: `TimeQuest/Program.cs:17-19`
- **Description**: `AddIdentity<ApplicationUser, IdentityRole<int>>()` is registered without configuring `options.Password.*` constraints. Per spec §5.4, authentication is delegated to Entra ID and local passwords are not supported. However, the Identity framework still exposes local-auth endpoints (via `AddDefaultTokenProviders`), and if a developer inadvertently enables local login, there would be no password complexity requirement.
- **Evidence**: `builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();`
- **Impact**: Informational while Entra ID is the sole auth path. Becomes a risk if local auth is ever enabled without updating the password policy.
- **Recommendation**: Either add `options.Password.RequireDigit = true; options.Password.RequiredLength = 12;` etc. as defence-in-depth, or remove `AddDefaultTokenProviders()` if local auth is definitively not needed.
- **Exploitable unauthenticated**: No

---

### S-006 [Info] Entra ID OIDC not yet wired — auth is a development stub

- **Category**: OWASP A07 — Identification and Authentication Failures
- **Location**: `TimeQuest/Program.cs` (no `AddAuthentication().AddOpenIdConnect(...)`)
- **Description**: The application uses ASP.NET Core Identity for local user tracking but has not yet configured Entra ID OIDC federation. `GetCurrentUserId()` returns 0 everywhere. The app is not production-deployable in this state. The architecture design (phase 2) specifies Entra ID SSO.
- **Impact**: Informational — this is a known pre-production gap documented in all phase summaries.
- **Recommendation**: Before any staging or production deployment, add Entra ID OIDC via `AddAuthentication().AddMicrosoftIdentityWebApp(...)` (Microsoft.Identity.Web package) and wire `GetCurrentUserId()` via `AuthenticationStateProvider`. The `AzureAd` section must reference Key Vault secrets, not appsettings values.
- **Exploitable unauthenticated**: No

---

## 4. Dependency review

| Package | Version | Known CVEs | Action |
|---|---|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.* | None known | Keep — track .NET 10 security advisories |
| Microsoft.EntityFrameworkCore.SqlServer | 10.* | None known | Keep |
| Microsoft.AspNetCore.Identity.UI | 10.* | None known | Keep |
| @playwright/test | ^1.48.0 | None known (dev-only) | Keep |
| @axe-core/playwright | ^4.10.0 | None known (dev-only) | Keep |

All packages are from trusted feeds (nuget.org, npmjs.com). No wildcard `10.*` versions introduce auto-major-upgrade risk given .NET 10 is the target framework. No high-CVE packages found.

---

## 5. Secret scan

- **Scanned for**: SQL connection strings with passwords, Azure Storage keys, Service Bus SAS keys, JWTs, GitHub PATs, AWS access keys, private keys, generic API keys.
- **Files scanned**: 150+ source files (.cs, .razor, .ts, .js, .json, .yaml, .yml). Skipped: bin/, obj/, node_modules/, .git/, .env.example, skill documentation.
- **Findings**: **(none detected)**

The `appsettings.json` LocalDB connection string uses `Trusted_Connection=True` (Windows integrated auth) — no password present. Reported separately as S-004 (Low) for configuration hygiene, not a secret leak.

---

## 6. Auth / AuthZ audit

This is a Blazor Server application. There are no REST API controllers — all "endpoints" are Blazor page components accessed via the router. Auth is enforced at three layers:

| Route | Required role | `[Authorize]` present | Resource-level scope check |
|---|---|---|---|
| /timesheet | All authenticated | Yes (`[Authorize]`) | `GetCurrentUserId()` stub — see S-002 |
| /approvals | TeamLead | Yes (`[Authorize(Roles = "TeamLead")]`) | Queue filtered by team membership |
| /approvals/{id} | TeamLead | Yes | **NO scope check in `GetTimesheetDetailAsync` — see S-001** |
| /financial/queue | FinancialAdmin | Yes (`[Authorize(Roles = "FinancialAdmin")]`) | FA exempt from scope per spec |
| /financial/queue/{id} | FinancialAdmin | Yes | FA exempt from scope per spec |
| /financial/lock | FinancialAdmin | Yes | FA exempt from scope |
| /financial/reports | FinancialAdmin | Yes | FA exempt from scope |
| /financial/export | FinancialAdmin | Yes | FA exempt from scope |
| /admin/teams | Administrator | Yes (`[Authorize(Roles = "Administrator")]`) | Service-layer scope guard (ScopeGuard) |
| /admin/teams/{id} | Administrator | Yes | Service-layer scope guard |
| /admin/projects | Administrator | Yes | Service-layer scope guard |
| /admin/projects/{id} | Administrator | Yes | Service-layer scope guard |
| /audit | FinancialAdmin, SystemAdmin | Yes | No resource-level filter needed (read-all) |
| /sysadmin/integrations | SystemAdmin | Yes | No resource-level filter needed |
| /403 | Anonymous | Not required | Static page |
| /404 | Anonymous | Not required | Static page |

`Routes.razor` wraps the router in `<CascadingAuthenticationState>` with `<AuthorizeRouteView>` redirecting unauthorized access to `/403`. This correctly prevents unauthenticated access to all protected pages.

`UseAuthentication()` is called before `UseAuthorization()` in the middleware pipeline — PASS.
`UseAntiforgery()` is present — PASS.
`UseHsts()` and `UseHttpsRedirection()` are present — PASS.

---

## 7. Cryptography audit

- **Password hashing**: Delegated to Entra ID; no local passwords stored. ASP.NET Identity is configured but Entra is the auth authority. Identity's default PBKDF2/SHA-256 would apply to any fallback local auth — acceptable.
- **Symmetric crypto**: Not used in application code. No custom encryption found.
- **Random source**: `RandomNumberGenerator` not referenced directly — no custom crypto operations found. EF Core handles concurrency tokens via SQL Server `rowversion`. No `System.Random` used for security purposes.
- **TLS**: `UseHttpsRedirection()` present in Program.cs; `UseHsts()` in non-dev environment. TLS minimum version is enforced at the App Service / Azure SQL level (to be confirmed in IaC — see §8).
- **No MD5/SHA1 for security**: Grep found no custom hash usage in source files.

---

## 8. IaC misconfig audit

Not applicable — deployment phase (phase 9) has not run. Bicep IaC files do not yet exist at `infra/`. This section will be populated after phase 9 completes. The architecture design (phase 2) specifies: `httpsOnly: true`, `minTlsVersion: '1.2'`, Managed Identity for Key Vault and Azure SQL, all secrets via Key Vault references.

---

## 9. Logging & monitoring audit

- **Sensitive data redacted**: PASS. Grepped all LogInformation/LogWarning/LogError calls in `src/`. Rejection comments, notes content, and ticket reference values are NOT logged. AuditService explicitly comments "Never log PII content (Notes, TicketRef, DisplayName values)" and logs only `{ActionType}/{ResourceType}/{ResourceId}/actorId` — no content values.
- **Auth events logged**: Partial — timesheet status transitions logged. Entra OIDC login events will be available in Entra's sign-in logs automatically once wired. Application-level failed auth attempts are not explicitly logged since there's no local auth path; once OIDC is wired, add `LogWarning` on `OnTokenValidationFailed`.
- **Application Insights configured**: Not yet — AI is in the architecture design (phase 2) but not yet in `Program.cs`. Must be added before staging deployment.
- **Alerts per spec §5.6**: Not yet configured — dependent on App Insights setup.

---

## 10. Post-deploy follow-ups (non-blocking)

### High priority (must fix before production)

1. **S-001**: Add team-scope check in `ApprovalService.GetTimesheetDetailAsync` for TeamLead callers. FinancialAdmin callers are exempt per spec.
2. **S-002**: Replace all `GetCurrentUserId() => 0` stubs with `AuthenticationStateProvider`-based claim extraction. Add null/parse guard.
3. **S-006**: Wire Entra ID OIDC (`AddMicrosoftIdentityWebApp`) before any staging or production deployment.

### Medium priority (next sprint)

4. **S-003**: Add rate limiting on export and report-generation routes (min: 5 calls/minute per user).
5. Add Application Insights telemetry to `Program.cs` (connection string via Key Vault reference).
6. Log Entra OIDC token-validation failures as `LogWarning` for security monitoring.

### Low priority (before go-live)

7. **S-004**: Replace the `appsettings.json` LocalDB connection string with `""` and document the Key Vault reference requirement in the deployment runbook.
8. Configure Identity password options as defence-in-depth or remove `AddDefaultTokenProviders()`.

VERDICT: APPROVED