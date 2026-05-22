# BA Critic — Iteration 2

## Functional findings (§§1–4)
1. [PASS] Vision — clear, specific, one sentence; names Harvest replacement and South African financial compliance requirement.
2. [PASS] Personas — five named personas (Team Member, Team Lead, Administrator, Financial Admin, System Admin) with roles and primary goals.
3. [PASS] Epics — six epics (E1–E6) numbered and aligned with user-prompt goals (time capture, approval, financial, admin, reporting, integrations).
4. [PASS] Features — all 17 features use "As a / I want / so that" format; each addresses user-prompt requirements.
5. [PASS] Acceptance criteria — all features carry testable acceptance criteria; no vague terms without measurable context (e.g., "flagged" specifies visual indicator; "validation" specifies minimum character count or threshold).
6. [PASS] Tasks — all tasks are ≤ 1 dev-day (entity creation, service method, component build); none describe multi-day work.

---

## NFR audit (§5)

| Sub-section | Present? | Testable? | Defaulted echoed in §7? | Notes |
|---|---|---|---|---|
| 5.1 Performance | yes | yes | n/a | Concrete numbers: p50/p95/p99 latencies (100ms / 300ms / 800ms); RPS (200 sustained, 600 peak); TTI < 3s; batch timeouts (30s reports, 60s export, 15min retry cycle). All measurable. |
| 5.2 Scalability | yes | yes | n/a | Concurrent users: 500 @ launch, 2,000 @ 12mo; data volume: ~5M rows @ 3yr, ~25GB total; single-region (Azure South Africa North) stated. All measurable. |
| 5.3 Availability & Reliability | yes | yes | yes | SLA 99.5% stated and marked `[DEFAULT — see OQ-003]`; RTO 4h / RPO 24h stated and marked `[DEFAULT — see OQ-004]`. Both defaults echoed in §7 with `NFR default applied:` prefix. Disaster recovery scope detailed (automated daily backups, 35-day retention, geo-redundant storage, monthly restore test). |
| 5.4 Security & Compliance | yes | yes | yes | AuthN: Entra ID + MFA (testable). Data classification: Confidential with specific field types (employee names, hours, rates, ticket refs, comments). Regimes: POPIA + GDPR named; flagged `[legal confirmation required — see OQ-002]` and echoed in §7 with `NFR default applied:` prefix. Data residency: Azure South Africa North (testable). Encryption: TLS 1.2 + ATDE + Key Vault (testable). Session: 30min idle + 8hr absolute marked `[DEFAULT — see OQ-010]` and echoed in §7. |
| 5.5 Auditing | yes | yes | n/a | Audited actions: time entry lifecycle, timesheet lifecycle, approvals (all levels), lock, export, user/project ops, login/logout, failed logins (comprehensive list). Record fields: actor (ID + name), action type, entity (type + ID), before/after JSON, UTC timestamp, source IP, correlation ID. Retention: 7 years (South Africa Companies Act requirement). Immutability: append-only; DELETE/UPDATE permissions revoked at database constraint level (testable, enforceable). Readers: Financial Admin, System Admin scoped. |
| 5.6 Observability | yes | yes | n/a | Log level: Warning (production) + Information (auth/transitions). Redaction list explicit: no passwords, tokens, session IDs, Notes text content, ticket reference content (testable policy). Retention: 30d hot (Application Insights) + 12mo cold (Archive tier). Metrics: 5 named (daily active users, submitted timesheets/week, queue depth by stage, export count/day, integration validation failure rate). Alerts: 5 named with thresholds (error rate > 1% in 5min, queue depth > 100 for 48h, export failure, integration offline > 30min, Azure SQL DTU/CPU > 80% for 10min). Tracing: Application Insights distributed tracing required across all service calls and background jobs (testable). |
| 5.7 Accessibility | yes | yes | n/a | Target standard: WCAG 2.1 AA (testable, verifiable). Keyboard-only navigation required for all core flows. Screen-reader support required; all components must carry ARIA labels and roles. No specific accommodations known at Phase 1; AA baseline provides broad coverage. |
| 5.8 Browser & Device Support | yes | yes | yes | Browsers: Chrome 120+, Edge 120+, Firefox 120+, Safari 17+ stated and marked `[DEFAULT — see OQ-011]`; echoed in §7 with `NFR default applied:` prefix. Mobile: responsive layout required (daily entry expected from mobile); no native iOS/Android app in scope (testable). Offline: explicitly not required (Blazor Server circuit requirement stated, testable). Viewport min: 320px (testable). |
| 5.9 Localisation & Internationalisation | yes | yes | n/a | Launch language: English (South African, testable). Future languages: none planned (explicit decision, testable). Date format: dd/MM/yyyy; time: 24-hour; currency: ZAR with space thousands separator per SANS standard (testable). RTL: not required. |
| 5.10 Maintainability & Delivery | yes | yes | n/a | Coverage: 80% service-layer minimum, 60% overall (testable). Build time: < 5min on CI (testable). Deploy frequency: multiple/day to dev, weekly to prod (testable, verifiable in pipeline). Documentation: README (local setup + migration) + runbook (deployment, backup restore, credential rotation) required (testable deliverables). |
| 5.11 Cost Constraints | yes | yes | n/a | **Blocker resolved in iteration 2:** §5.11 now states "no cap set — stakeholders explicitly confirmed no fixed budget limit; architect must present a SKU-level cost estimate for dev and prod and Financial Admin must approve it before infrastructure is provisioned." This is an explicit confirmed waiver (not a placeholder), with mandatory architect estimate + stakeholder sign-off before infra deployment. Testable and compliant with blocking rule. OQ-001 properly reflects this decision. |

---

## Compliance-hint check

**Feature text triggers:**
- F1.1 (time entry): "billing"
- F2.1–F2.4 (approval workflow): implicit handling of billing-sensitive data
- F3.2 (export): "billing template format"
- F5.1 (reconciliation): "invoicing and financial audit"
- F5.2 (audit trail): "seven-year audit trail to satisfy South African financial compliance"
- Vision (§1): "South African financial compliance requirements"
- §5.4 data classification: "billing rate / amounts"

**§5.4 regime named:** POPIA (South Africa), GDPR (EU). Correctly identified based on South African operation and EU client exposure. Flagged for legal confirmation in OQ-002 with `NFR default applied:` prefix in §7.

**Compliance coherence:** **PASS** — POPIA and GDPR are correctly named and justified by feature triggers. Properly flagged for legal sign-off before architecture phase (OQ-002). This is a Phase 1 dependency, not a blocker.

---

## Cross-cutting findings (§§6–7, consistency)

### Section 6 — Out of Scope
- Present: yes
- Content: 8 items listed (payroll, invoice generation, resource allocation, native mobile, offline time entry, multi-currency, HR leave integration, self-service registration)
- Consistency: all items align with user-prompt constraints; self-service registration is a sensible exclusion for Entra-provisioned users and does not contradict user-prompt

### Section 7 — Open Questions
- Present: yes
- Count: 12 OQs listed (OQ-001 to OQ-012)

**NFR defaults echoed correctly with `NFR default applied:` prefix:**
  - OQ-001: §5.11 cost — "no cap set — stakeholders explicitly confirmed no fixed budget limit" (explicit waiver, not default) ✓
  - OQ-002: §5.4 compliance regimes — `[legal confirmation required — see OQ-002]` ✓
  - OQ-003: §5.3 SLA 99.5% — `[DEFAULT — see OQ-003]` ✓
  - OQ-004: §5.3 RTO/RPO — `[DEFAULT — see OQ-004]` ✓
  - OQ-010: §5.4 session policy (30min / 8hr, single concurrent) — `[DEFAULT — see OQ-010]` ✓
  - OQ-011: §5.8 browser versions — `[DEFAULT — see OQ-011]` ✓

**Non-NFR questions (design, implementation, integrations):**
  - OQ-005: Export billing template column spec (blocks F3.2)
  - OQ-006: Overtime threshold value confirmation (e.g., is 10 hours/day global or per-project configurable?)
  - OQ-007: Leave and public holiday source (admin calendar vs. HR integration vs. manual flag)
  - OQ-008: Admin scope assignment mechanism (initial provisioning + removal scenario)
  - OQ-009: Integration credentials (PAT, OAuth2, API key + rotation policy)
  - OQ-012: Partial-week leave flag UX (toggle vs. free-text vs. selector)

### Consistency checks
- **Feature numbering:** F1.1–F1.3, F2.1–F2.4, F3.1–F3.2, F4.1–F4.3, F5.1–F5.2, F6.1. Numbering is consistent; no gaps or orphans.
- **Persona references:** All five personas (Team Member, Team Lead, Administrator, Financial Admin, System Admin) are referenced in features. No orphaned personas; complete coverage.
- **Epic-to-Feature alignment:**
  - E1 (Time Capture & Submission): F1.1, F1.2, F1.3 ✓
  - E2 (Multi-Level Approval Workflow): F2.1, F2.2, F2.3, F2.4 ✓
  - E3 (Financial Lock & Export): F3.1, F3.2 ✓
  - E4 (Administration & RBAC): F4.1, F4.2, F4.3 ✓
  - E5 (Reporting & Audit Trail): F5.1, F5.2 ✓
  - E6 (Integrations): F6.1 ✓
- **Terminology:** "timesheet", "time entry", "submission", "approval", "locked" used consistently. Role terminology matches user-prompt exactly.
- **Scope alignment with user-prompt:** All actor roles, status lifecycle, approval workflow, role-scoped visibility, Financial Admin extra approval, immutability, integrations, reconciliation, 7-year audit trail, and out-of-scope items are faithfully represented in spec.

---

## Required fixes (if BLOCKED)

None. The iteration 1 blocker (§5.11 cost) has been resolved.

---

## Notes (non-blocking)

1. **OQ-002 (compliance legal sign-off) is properly flagged** — POPIA and GDPR are named in §5.4, and legal confirmation is correctly called out as a prerequisite for architecture. This is a Phase 1 dependency, not a blocker.

2. **OQ-001 (cost waiver) is now explicit** — The rewrite makes clear that stakeholders have confirmed "no fixed budget limit" and that the architect will present a SKU-level estimate for Financial Admin approval before infra is deployed. This satisfies the blocking rule: cost is either a confirmed number OR a confirmed explicit waiver with cost estimation + stakeholder approval as a prerequisite.

3. **OQ-005 (export template format) is a Phase 5 (Backend Developer) blocker** — F3.2 references "the billing template format" but the exact CSV columns are not specified. Correctly flagged in §7. This must be resolved before implementation, not a Phase 1 defect.

4. **Feature count and complexity** — 17 features across 6 epics is well-scoped. No invented features beyond user-prompt; all align with stated goals.

5. **Acceptance criteria quality** — Specific enough to guide development (exact validation messages, HTTP status codes, character minimums) without overspecifying implementation. Strong practice.

6. **Audit trail and compliance design** — Before/after JSON snapshots, 7-year retention tied to Companies Act requirement, immutability enforced at database constraint level (DELETE/UPDATE revocation). Strong compliance foundation.

7. **Role-scoped visibility (F4.3)** — Correctly specifies that Financial Admin is exempt from scope restrictions, preventing bypass. Design is sound.

8. **Session and security defaults** — 30min idle + 8hr absolute timeout, Entra MFA required, TLS 1.2 + ATDE + Key Vault. Defaults are secure for an internal billing tool.

---

VERDICT: APPROVED
