---
name: security-checklist
description: The audit checklist used by the Security Reviewer agent (phase 8). Covers OWASP Top 10 with concrete ASP.NET / Blazor checks, Blazor-specific concerns (anti-forgery, MarkupString, Identity sessions), ASP.NET / .NET specifics (password policy, Managed Identity, AllowAnonymous slips), IaC misconfig checks (HTTPS, TLS, Managed Identity, Key Vault references, network exposure), secret-scan regex patterns, and crypto audit items. Invoke when scanning the codebase or answering questions about what to check.
---

# Security Audit Checklist

Work through every item. Note in the report which applied and which didn't.

## OWASP Top 10 (2021)

### A01 Broken Access Control
- Object-level authz: every fetch/update/delete by ID checks ownership (`o.UserId == User.GetId()` or policy)
- Role checks on every protected endpoint (`.RequireAuthorization()` or `[Authorize]`)
- No `[AllowAnonymous]` on endpoints that handle sensitive data
- Return 404 (not 403) for resources the user doesn't own - avoids enumeration / info leak
- Admin endpoints behind a role/policy guard

### A02 Cryptographic Failures
- TLS enforced (`UseHttpsRedirection()` present; HSTS header in non-Dev)
- Min TLS 1.2 (or 1.3) in IaC
- No secrets in code / configs - all via Key Vault references or env vars
- Password hashing modern (PBKDF2 / Argon2 / bcrypt) - ASP.NET Identity default is fine
- No MD5 / SHA1 for any security purpose
- Crypto operations use `RandomNumberGenerator` not `System.Random`

### A03 Injection
- EF Core LINQ is safe by default
- Flag any `FromSqlRaw` with string concatenation
- Use `FromSqlInterpolated` for parameterised raw SQL
- No `Process.Start` with user input
- No `eval` / dynamic LINQ with user-controlled strings

### A04 Insecure Design
- Rate limiting on auth endpoints (login, password reset)
- Account lockout after N failed attempts
- Idempotency keys on financial / destructive operations
- No predictable IDs (sequential ints) on resources where enumeration leaks info - use Guid

### A05 Security Misconfiguration
- HTTPS only
- HSTS header in production
- Dev exception page disabled in production (`if (!app.Environment.IsDevelopment())`)
- Default credentials changed from seed values in non-dev environments
- CORS not wide-open (`AllowAnyOrigin` flag)
- Detailed error messages disabled in production

### A06 Vulnerable & Outdated Components
- Run `.csproj` review against known-vuln packages
- `package.json` (Playwright deps, frontend) review
- Pin versions; don't auto-update from untrusted feeds
- Note: many CVEs are low-impact - flag by realistic exploitability, not just CVSS

### A07 Identification & Authentication Failures
- Password policy matches spec §5.4 (min length, complexity)
- Session timeout matches spec §5.4
- MFA support if spec demands
- Idle timeout enforced
- Session fixation: identifier regenerated on login

### A08 Software & Data Integrity Failures
- Dependency sources trusted (nuget.org only, no random feeds)
- No auto-deploy from external sources without verification
- Container images from trusted registries
- Code signing / SBOM where spec demands

### A09 Security Logging & Monitoring
- Auth events logged (success + failure)
- Sensitive data NOT logged (passwords, full tokens, PII per spec §5.6)
- Failed authz attempts logged for audit
- Application Insights / log sink configured

### A10 SSRF
- Any server-side HTTP call with user-supplied URL?
- If yes, validate against allow-list / block private IP ranges
- Common vector: webhook URLs, image fetching, PDF generation from URLs

## Blazor-specific

- `app.UseAntiforgery()` present in Program.cs
- Razor expressions auto-escape; flag `@((MarkupString)...)` with untrusted input
- `[Authorize]` attribute on protected pages
- `<AuthorizeView>` guards for role-based UI elements
- No `DbContext` directly in components (Frontend Critic concern but verify)
- Interactive Server sessions: no PII held in component fields longer than needed
- Component disposes `CancellationTokenSource` to avoid leaking circuit resources
- No JS interop with `eval` or innerHTML with user input

## ASP.NET / .NET-specific

- Identity password requirements set in `AddIdentity` options (`RequireDigit`, `RequiredLength`, etc.)
- Connection strings via Key Vault reference, not appsettings.json
- Managed Identity used for Azure resource access (DB, KV, Storage)
- No `AllowAnonymous` where it shouldn't be - grep for it in protected resource controllers
- Anti-forgery middleware before endpoint mapping in pipeline order
- `app.UseAuthentication()` BEFORE `app.UseAuthorization()`
- Custom middleware doesn't bypass authentication

## Secret scan patterns (regex sweeps to run)

Use Grep with these patterns across source / configs / pipelines:

| Pattern | What it catches |
|---|---|
| `Server=.*Password=` | SQL connection string with embedded password |
| `AccountKey=[A-Za-z0-9+/=]{60,}` | Azure Storage key |
| `SharedAccessKey=[A-Za-z0-9+/=]+` | Service Bus / Event Hub SAS |
| `eyJ[A-Za-z0-9_-]{10,}\.eyJ` | JWT |
| `ghp_[A-Za-z0-9]{36}` | GitHub PAT |
| `ghs_[A-Za-z0-9]{36}` | GitHub app token |
| `xox[bsop]-[A-Za-z0-9-]+` | Slack token |
| `AKIA[A-Z0-9]{16}` | AWS access key |
| `-----BEGIN (RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----` | Private key |
| `(api[_-]?key\|apikey)["':\s=]+[A-Za-z0-9_-]{20,}` | Generic API key |

Where to scan:
- All source files (`.cs`, `.razor`, `.ts`, `.js`)
- All configs (`appsettings*.json`, `.env*`, `*.yaml`, `*.yml`)
- All pipeline files (`.github/workflows/*.yml`, `azure-pipelines.yml`)
- All Bicep / Terraform (`*.bicep`, `*.bicepparam`, `*.tf`)
- Test fixtures (sometimes the worst offenders)

Skip:
- `.env.example` (placeholder values OK)
- Build artifacts (`bin/`, `obj/`, `node_modules/`)
- Locked-down docs that intentionally show example formats

## IaC misconfig checklist (if `infra/` is present)

- `httpsOnly: true` on every App Service
- `minTlsVersion: '1.2'` (or 1.3) on App Service + Azure SQL + Storage
- App Service has Managed Identity assigned
- Key Vault access policies use `roleAssignments` with named principals (no `*` wildcards)
- SQL Server `publicNetworkAccess: 'Disabled'` (or with firewall rules) if §5.4 calls for private
- Storage account `allowBlobPublicAccess: false`
- Key Vault `enabledForDeployment: false` and `enabledForDiskEncryption: false` unless needed
- No hardcoded passwords or connection strings - all `@Microsoft.KeyVault(...)` references
- Diagnostic settings export to Log Analytics

## What is NOT a finding (don't waste a slot)

- Code style nits without security impact
- Theoretical CVEs in dependencies that aren't reachable from the app
- Missing CSP headers on a static marketing page (Low at most, not High)
- General DevOps suggestions ("you should add monitoring") without a concrete gap

## How to be specific

Bad finding: "Authorisation might be missing somewhere"
Good finding: `OrdersEndpoints.cs:42 returns any order without ownership check`

Bad finding: "Crypto looks weak"
Good finding: `UserService.cs:88 hashes passwords with SHA1 (one-way but trivially brute-forceable for short inputs)`

Every Critical / High finding must have: file:line + a one-line evidence snippet + an exploit scenario.
