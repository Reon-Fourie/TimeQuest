---
name: security-report-template
description: The report.md template the Security Reviewer agent (phase 8) writes. Includes scope, findings summary table, per-finding format (severity / category / location / evidence / impact / recommendation / exploitable-unauthenticated), dependency review, secret scan, auth audit, crypto audit, IaC misconfig audit, logging audit, post-deploy follow-ups, severity rubric, verdict rules. Invoke ONLY when writing the report.
---

# Security Review Report Template

The orchestrator parses the LAST LINE of this file for `VERDICT: APPROVED` or `VERDICT: BLOCKED`. Get the verdict line right.

```markdown
# Security Review - <Project Name>

## 1. Scope reviewed
- **Backend**: <list of dirs/projects scanned>
- **Frontend**: <list>
- **Infra**: <yes/no, list paths or "not present yet">
- **Pipelines**: <yes/no, list paths or "not present yet">
- **Dependencies**: <.csproj / package.json files reviewed>
- **Configs**: <appsettings*.json, .env files reviewed>

## 2. Findings summary
| Severity | Count |
|---|---|
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Info | 0 |

## 3. Findings detail
One block per finding. Number sequentially across all severities.

### S-001 [Critical] <Short title>
- **Category**: OWASP A01 - Broken Access Control
- **Location**: `src/<Project>.Api/OrdersEndpoints.cs:42`
- **Description**: GET /api/orders/{id} returns any order without checking ownership.
- **Evidence**:
  \`\`\`csharp
  return await _db.Orders.FindAsync(id);
  \`\`\`
- **Impact**: Authenticated user can read any other user's orders by guessing IDs.
- **Recommendation**: Filter by `UserId == User.GetId()` or enforce via authorization policy.
- **Exploitable unauthenticated**: No

(repeat for S-002, S-003, ...)

## 4. Dependency review
| Package | Version | Known CVEs | Action |
|---|---|---|---|
| Newtonsoft.Json | 12.0.3 | CVE-2024-XXXXX (high) | upgrade to 13.0.3 |
| (or "(none flagged)") | | | |

## 5. Secret scan
- **Scanned for**: AWS keys, Azure SAS / connection strings, JWT-shaped strings, GitHub PATs (ghp_), Azure DevOps PATs, private keys (`-----BEGIN`), high-entropy strings in source/configs/pipelines.
- **Findings**: <list of file:line + redacted match, or "(none)">

## 6. Auth / AuthZ audit
Endpoint inventory:

| Endpoint | Method | Required role | `[Authorize]` present? | Resource-level check? |
|---|---|---|---|---|
| /api/orders | GET | (any auth) | yes (group) | filters by current user |
| /api/orders/{id} | GET | (any auth) | yes (group) | **NO** - see S-001 |

## 7. Cryptography audit
- **Password hashing**: <e.g. ASP.NET Identity defaults = PBKDF2/SHA-256, 10000 iterations>
- **Symmetric crypto**: <algorithm, mode, or "not used">
- **Random source**: <`RandomNumberGenerator` vs `Random` - flag any `Random` used for security>
- **TLS**: <enforced via UseHttpsRedirection? HSTS? min TLS version configured?>

## 8. IaC misconfig audit
(If `infra/` doesn't exist yet, write "Not applicable - deployment phase has not run.")

- **HTTPS only on App Service**: <yes/no>
- **Min TLS version**: <value>
- **Managed Identity for DB / Key Vault**: <yes/no>
- **Secrets in app settings vs Key Vault references**: <state>
- **Network exposure**: <Public / Private endpoints>
- **RBAC role assignments minimal-privilege**: <yes/no>
- **Public IP on SQL / Storage**: <yes/no>

## 9. Logging & monitoring audit
- **Sensitive data redacted**: <list of redacted fields or "no redaction wired">
- **Failed auth attempts logged**: <yes/no>
- **Application Insights / log sink configured**: <yes/no>
- **Alerts wired per spec §5.6**: <yes/no + list>

## 10. Post-deploy follow-ups (non-blocking)
- <High findings requiring auth (recorded above) that don't block but should be fixed>
- <Medium / Low items worth fixing in next sprint>

VERDICT: APPROVED
```

## Verdict rules (MANDATORY - last line of file)

- **`VERDICT: BLOCKED`** if ANY finding is severity **Critical**.
- **`VERDICT: BLOCKED`** if any finding is severity **High** AND exploitable without authentication (anonymous attacker).
- **`VERDICT: APPROVED`** otherwise. Auth-gated High findings are recorded but go on §10 post-deploy follow-ups.

The orchestrator parses the last line with regex `^VERDICT:\s*(APPROVED|BLOCKED)\s*$`. Don't add anything after the verdict line.

## Severity rubric
- **Critical** - Direct data breach, RCE, full auth bypass, unrestricted privilege escalation, hardcoded production secret. Blocks deployment unconditionally.
- **High** - Specific data exposure, missing authz on a sensitive endpoint, weak crypto on sensitive data, exploitable XSS / CSRF / SSRF, IaC public exposure of a DB. Blocks if exploitable anonymously.
- **Medium** - Defence-in-depth gap (missing rate limiting, weak password policy, missing HSTS), info disclosure of low-sensitivity data, missing CSP header, dependency vuln rated Medium.
- **Low** - Code-quality with security implications (over-logging, missing input length caps), outdated but not vuln dependency.
- **Info** - Recommendations and observations.

## Required content invariants
- All 10 sections present (write "Not applicable - <reason>" instead of omitting).
- Every Critical / High finding has all of: category, location, evidence, impact, recommendation, exploitable-unauthenticated flag.
- Findings numbered sequentially (S-001, S-002, ...).
- Last line is exactly `VERDICT: APPROVED` or `VERDICT: BLOCKED`.
