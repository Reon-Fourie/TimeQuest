# Architect Critic — Iteration 1

## Spec coverage

| Epic | Feature(s) | Covered by | Status |
|---|---|---|---|
| E1: Time Capture & Submission | F1.1, F1.2, F1.3 | `TimeEntry` / `WeeklyTimesheet` entities in Domain; `TimeEntryService`, `WeeklyTimesheetService` in Infrastructure; Blazor components in Web; `TimeEntryRepository` in Infrastructure/Repositories | PASS |
| E2: Multi-Level Approval Workflow | F2.1, F2.2, F2.3, F2.4 | `ApprovalService` (Lead + Financial paths) + `ScopeGuard` in Infrastructure; two-stage status model (`Submitted → ApprovedByLead → FinancialApproved`); `ApprovalRecord` entity implied; `TimesheetRepository.GetPendingForTeamLead` | PASS |
| E3: Financial Lock & Export | F3.1, F3.2 | `ApprovalService.FinalLock`, `ExportService`, Azure Blob (CSV staging 24 h), `AuditService` wired into export; Blazor `FinalLockQueuePage` + `ExportPage` implied | PASS |
| E4: Administration & RBAC | F4.1, F4.2, F4.3 | Five application roles seeded; `ScopeGuard` service explicitly called out in §5 cross-cutting; `TeamService`, `ProjectService`, `UserService` implied in Infrastructure | PASS |
| E5: Reporting & Audit Trail | F5.1, F5.2 | `ReportService` (daily/weekly/monthly over Locked entries); `AuditService` append-only to `AuditEvents`; DELETE/UPDATE permissions revoked at DB level; 7-year WORM retention via Blob; Log Analytics 90-day hot; `AuditLogPage` component implied | PASS |
| E6: Integrations | F6.1 | `IntegrationService` with DevOps/JIRA/Linear adapters in Infrastructure/Integrations; `IHostedService` background retry (15-min interval); `IntegrationStatusPage` component implied | PASS |

All six epics have clear architectural homes. No epic is orphaned.

---

## Role mapping audit

The spec defines five personas: Team Member, Team Lead, Administrator, Financial Admin, System Admin.  
The design defines five application roles: `TeamMember`, `TeamLead`, `Administrator`, `FinancialAdmin`, `SystemAdmin`.  
Mapping is 1:1. **PASS.**

---

## Simplicity audit

- **App Service (Linux) + EF Core + Azure SQL** — appropriate for a single-team line-of-business Blazor Server app. No over-engineering.
- **IHostedService for background jobs** — correct choice. The spec has exactly two background tasks (ticket validation retry every 15 min; audit export to Blob). Azure Functions / Service Bus would be unjustified overhead. PASS.
- **In-memory `IMemoryCache`** — accepted over Redis. The p95 target of 300 ms and user count (500–2 000) do not require a distributed cache. Rejection rationale in §3 is sound. PASS.
- **No APIM / Front Door** — correctly rejected. Single Blazor client, single region, no per-consumer rate limiting. PASS.
- **No AKS / Container Apps** — correctly rejected. Container Apps cold-start is incompatible with persistent Blazor Server circuits. PASS.
- **No Azure Service Bus** — correctly rejected. All approval transitions are single-producer/single-consumer; no fan-out. PASS.
- **No Azure App Configuration** — acceptable. Single team, no runtime feature-flag requirement in spec. PASS.
- **Application Insights + Log Analytics Workspace (~$92/month combined in dev)** — this is the one simplicity flag. At 2 GB/day the dev estimate of $46 + $46 is high for a developer testing environment. The spec does not require full-scale observability in dev. A lower ingestion budget (0.5 GB/day) or a free App Insights tier could cut this to ~$15–20/month. This is not a blocker (cost audit below addresses it) but worth noting.
- **Blob Storage for CSV export + audit archive** — correct; no alternative needed. PASS.
- **No Private Endpoints** — rejection rationale is spec-aligned: POPIA/GDPR obligations are met by TDE + RBAC + HTTPS. 99.5% SLA does not mandate network isolation. PASS.

No unjustified services detected. The stack is lean and every service traces to a spec requirement.

---

## Cost audit

| Line item | Dev est. | Assessment |
|---|---|---|
| App Service Plan B2 (Linux) | ~$26/month | Reasonable for always-on Blazor Server |
| Azure SQL GP Serverless 1–2 vCore, auto-pause 1h | ~$40/month | Reasonable; auto-pause saves cost when idle |
| Azure Blob Storage Standard LRS | ~$2/month | Fine |
| Application Insights (workspace-based, ~2 GB/day) | ~$46/month | Elevated for dev — see note below |
| Log Analytics Workspace (~2 GB/day) | ~$46/month | Elevated for dev — see note below |
| Azure Key Vault Standard | ~$0.03/month | Negligible |
| **Total dev** | **~$160/month** | **BORDERLINE** |

**Note:** The $92/month combined observability cost is elevated for a dev environment. 2 GB/day is a production-level ingestion rate; a typical dev environment generates 0.1–0.5 GB/day. Reducing the ingestion estimate to 0.5 GB/day would drop observability costs to approximately $23/month and bring the dev total to ~$90/month. The architect should revisit the ingestion estimate before presenting to Financial Admin. This is a **non-blocking note** — the spec explicitly states no budget cap and requires Financial Admin approval of the estimate (OQ-001), so the process control is already in place.

Overall the cost level is not a blocker. No SKU is above standard tier for dev without justification (SQL GP Serverless is the standard serverless tier; B2 App Service is Basic tier). The prod estimate (~$380–$450/month) is proportionate and well-reasoned.

**Dev env total: ~$160/month — REASONABLE (minor ingestion estimate concern noted above, non-blocking)**

---

## Security & ops

| Concern | Design response | Assessment |
|---|---|---|
| Secrets in Key Vault | Yes — KV Standard, Managed Identity, KV references in App Service config, no secrets in appsettings | PASS |
| Managed Identity | Yes — App Service → Key Vault exclusively via Managed Identity | PASS |
| HTTPS-only | Yes — HTTPS-only enforced at App Service level, HTTP → HTTPS redirect, TLS 1.2 minimum | PASS |
| AuthN scheme | Yes — Azure Entra ID OIDC, ASP.NET Core Identity federated to Entra, MFA via Conditional Access | PASS |
| AuthZ (role-based) | Yes — five roles seeded, Entra group claims mapped to roles | PASS |
| Scope enforcement | Yes — `ScopeGuard` at service layer (not UI-only); throws `ScopeViolationException` → HTTP 403 | PASS |
| Audit immutability | Yes — DELETE/UPDATE revoked at DB constraint level; WORM Blob archive; 7-year retention | PASS |
| Session policy | Yes — 30-min idle, 8-hour absolute via Data Protection + Entra token lifetime | PASS |
| Data at rest | Yes — Azure SQL TDE; Blob Storage encrypted at rest by default | PASS |
| Data residency | Yes — all services in South Africa North; no cross-border transfer | PASS |
| Health checks | Yes — `/healthz` probes SQL and Key Vault; probed by App Service liveness + AI availability tests | PASS |
| Logging sink | Yes — Application Insights via OpenTelemetry + Serilog; Log Analytics centralised ingestion | PASS |
| Deploy target | Yes — App Service (Linux), environments table covers dev/qa/staging/prod | PASS |
| Sensitive data logging | Not explicitly addressed | NOTE — see below |

**Note on sensitive data logging:** The spec §5.6 explicitly states: "MUST NEVER be logged: passwords, tokens, session IDs, the text content of time entry Notes (may be commercially sensitive), ticket reference content in plain-text form." The design §5 mentions log level and audit events but does not call out a data-scrubbing or structured-logging policy that prevents Notes and ticket references from leaking into Application Insights traces. This should be addressed — either in the design or delegated to the Backend Developer as an implementation constraint. **Non-blocking** (the spec requirement exists; the design just doesn't repeat it).

**No security gaps that would block delivery.** The design correctly implements every §5.4 and §5.5 requirement.

---

## Role name discrepancy (minor)

The spec uses persona names: Team Member, Team Lead, Administrator, Financial Admin, System Admin.  
The design maps these to application roles: `TeamMember`, `TeamLead`, `Administrator`, `FinancialAdmin`, `SystemAdmin`.  

The CLAUDE.md lists three seeded roles (`Admin`, `Manager`, `Employee`) from the XP/badge MVP design, which predates this spec. The design correctly supersedes this with five roles matching the spec. The Data Designer and Backend Developer should be aware that the CLAUDE.md role list is stale relative to this spec. **Non-blocking note** — architectural design is correct.

---

## Open questions passed to Data Designer (§8)

The design raises seven well-formed data questions (AuditEvents isolation, soft-delete strategy, WeeklyTimesheet creation trigger, TicketValidationStatus column type, approval queue index strategy, leave/holiday flag data model, concurrency control). All seven are appropriate for the Data Designer phase and do not represent blockers at the architecture level.

---

## Required fixes (if BLOCKED)

None. No blocking issues found.

---

## Notes (non-blocking)

1. **Observability ingestion estimate:** 2 GB/day in dev is likely overstated. Revise to 0.5 GB/day for the Financial Admin cost presentation to avoid sticker shock; dev total would drop to ~$90/month.
2. **Sensitive data logging policy:** Add an explicit note (or design decision) that structured logging is configured to exclude `TimeEntry.Notes`, `TicketReference` content, session tokens, and JWT claims from Application Insights traces. Delegate as a hard implementation constraint to the Backend Developer.
3. **CLAUDE.md role list is stale:** CLAUDE.md §Key architectural decisions lists three roles (`Admin`, `Manager`, `Employee`). The design and spec both define five roles. The CLAUDE.md should be updated to reflect the correct role names — no functional impact on the architecture.
4. **ARR Affinity note:** §7 correctly enables ARR Affinity for Blazor circuit pinning in prod (scale-out to 2 instances). Confirm this is also set in dev/QA even though dev runs a single instance, to avoid a configuration drift surprise when moving to multi-instance.
5. **`ApprovalRecord` entity not explicitly named in §4 project structure:** The spec AC for F2.2 and F2.4 requires persisting approver ID, timestamp, and rejection comments. The design references this concept in §5 (ScopeGuard, ApprovalService) but the `ApprovalRecord` entity is not listed in the Domain/Entities tree. Not a blocker — the Data Designer will define it — but worth flagging for traceability.
6. **Export CSV column structure (OQ-005):** The design correctly defers to `ExportService.GenerateLockedExport` but the billing template column format is still an open question per spec OQ-005. The architect should flag this as a prerequisite for the Backend Developer phase.

VERDICT: APPROVED
