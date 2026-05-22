# BA Critic — Iteration 1

## Functional findings (§§1–4)

1. [PASS] Vision — clear, specific, under 3 sentences; differentiates from Harvest; names South African compliance requirement.
2. [PASS] Personas — five named personas (Team Member, Team Lead, Administrator, Financial Admin, System Admin) with roles and primary goals stated.
3. [PASS] Epics — six epics (E1–E6) numbered and aligned with user-prompt goals (time capture, approval, financial, admin, reporting, integrations).
4. [PASS] Features — all features use "As a / I want / so that" format; 17 features across all epics.
5. [PASS] Acceptance criteria — all features carry acceptance criteria in testable form; no vague terms without context (e.g. "flagged" has a clear visual indicator; "reviewed" specifies validation notes).
6. [PASS] Tasks — all tasks are <= 1 dev-day (entity creation, service implementation, component build); none describe multi-day work like "implement authentication system".

---

## NFR audit (§5)

| Sub-section | Present? | Testable? | Defaulted echoed in §7? | Notes |
|---|---|---|---|---|
| 5.1 Performance | yes | yes | n/a | Concrete numbers: p50/p95/p99 latencies in ms; RPS throughput; TTI in seconds; batch job timeouts in seconds/minutes. All measurable. |
| 5.2 Scalability | yes | yes | n/a | Concurrent users: 500 @ launch, 2K @ 12mo; data volume: ~5M rows @ 3yr, ~25GB total; single-region stated. All measurable. |
| 5.3 Availability & Reliability | yes | yes | yes | SLA 99.5% stated but marked `[DEFAULT — see OQ-003]`; RTO 4h / RPO 24h stated but marked `[DEFAULT — see OQ-004]`. Both defaults echoed in §7. Disaster recovery scope detailed (automated backups, geo-redundant storage, monthly restore test). |
| 5.4 Security & Compliance | yes | yes | yes | Authent mechanism: Entra ID + MFA (testable). Data classification: Confidential (testable). Regimes: POPIA + GDPR identified but marked `[legal confirmation required — see OQ-002]`. Data residency: SA North (testable). Encryption: TLS 1.2 + ATDE + Key Vault (testable). Session: 30min idle + 8hr absolute, but marked `[DEFAULT — see OQ-010]` and echoed in §7. |
| 5.5 Auditing | yes | yes | n/a | Audited actions listed (create/edit/delete, submit, all approval levels, lock, export, login, project ops). Record fields: actor, action type, entity, before/after JSON, timestamp, IP, correlation ID. Retention: 7 years (testable, tied to Companies Act). Immutability: append-only with database-level `DELETE`/`UPDATE` revocation (testable). |
| 5.6 Observability | yes | yes | n/a | Log level: Warning + Info for auth/transitions (testable). Redaction list explicit: no passwords/tokens/session IDs/Notes content/ticket refs (testable). Retention: 30d hot + 12mo cold (testable). Metrics: 5+ named (DAU, submitted timesheets, queue depth, export count, validation failure rate). Alerts: 4+ named with thresholds (error rate > 1% in 5min, queue depth > 100 for 48h, export failure, integration offline > 30min, SQL resource > 80% for 10min). Tracing: Application Insights distributed tracing required. All measurable. |
| 5.7 Accessibility | yes | yes | n/a | WCAG 2.1 AA named (testable standard). Keyboard-only navigation required. Screen-reader / ARIA required. |
| 5.8 Browser & Device Support | yes | yes | yes | Browsers: Chrome 120+, Edge 120+, Firefox 120+, Safari 17+ stated but marked `[DEFAULT — see OQ-011]` and echoed in §7. Mobile: responsive layout required, no native app (testable). Offline: not required for Blazor Server (testable). Viewport min: 320px (testable). |
| 5.9 Localisation & Internationalisation | yes | yes | n/a | Launch language: English (South African). Future: none planned (testable / explicit). Formats: dd/MM/yyyy, 24-hour, ZAR with space thousands separator per SANS (testable). RTL: not required. |
| 5.10 Maintainability & Delivery | yes | yes | n/a | Coverage: 80% service-layer, 60% overall (testable). Build time: < 5min on CI (testable). Deploy frequency: multiple/day to dev, weekly to prod (testable). Documentation: README + runbook required (testable). |
| 5.11 Cost Constraints | yes | **no** | yes | Cost sub-section present but value is not a number: "not yet confirmed — architect must present dev + prod cost estimate". Marked `[see OQ-001]` and echoed in §7 as `NFR default applied: §5.11 — no target set`. **This is a BLOCKER: cost must be a confirmed number, not "to be estimated".** |

---

## Compliance-hint check

**Feature text triggers:**
- F1.1 (time entry): "billing" (commercial sensitive data)
- F2.1–F2.4 (approval workflow): implicit handling of billing-sensitive timesheets
- F3.2 (export): "billing template format"
- F5.1 (reconciliation): "invoicing and financial audit"
- F5.2 (audit trail): "seven-year audit trail to satisfy South African financial compliance"
- Vision (§1): "South African financial compliance requirements"
- §5.4 data classification: "billing rate / amounts"

**§5.4 regime named:** POPIA (South Africa), GDPR (EU). However, POPIA/GDPR are named but flagged `[legal confirmation required — see OQ-002]`, meaning they are assumed from context, not confirmed.

**Compliance coherence:** **CONDITIONAL PASS** — POPIA and GDPR are correctly identified based on South African operation + EU client exposure (billing data at scale). However, the spec correctly flags this as unconfirmed in OQ-002. Since OQ-002 is echoed in §7 with the `NFR default applied:` prefix for §5.4, and the spec explicitly calls out legal sign-off as a prerequisite, this is not a standalone blocker — it is properly flagged for confirmation before architecture. If POPIA/GDPR were *ignored* in §5.4 despite the compliance hints, that would be a blocker; instead, they are named and flagged for confirmation, which is acceptable for Phase 1.

---

## Cross-cutting findings (§§6–7, consistency)

### Section 6 — Out of Scope
- Present: yes
- Content: 7 items listed (payroll, invoice generation, resource allocation, native mobile, offline time entry, multi-currency, HR leave integration, self-service registration)
- Consistency: all items referenced in user-prompt match spec (no invented additions); self-service registration is not in user-prompt but is a sensible exclusion for Entra-provisioned users

### Section 7 — Open Questions
- Present: yes
- Count: 12 OQs listed (OQ-001 to OQ-012)
- **NFR defaults echoed correctly:**
  - OQ-001: `[NFR default applied: §5.11 — no target set]` ✓
  - OQ-002: `[NFR default applied: §5.4 — regimes assumed from context]` ✓
  - OQ-003: `[NFR default applied: §5.3 — 99.5%]` ✓
  - OQ-004: `[NFR default applied: §5.3 — RTO 4h, RPO 24h]` ✓
  - OQ-010: `[NFR default applied: §5.4 — single concurrent session]` ✓
  - OQ-011: `[NFR default applied: §5.8]` ✓

- **Non-NFR questions** (design, implementation, integrations):
  - OQ-005: Export template format (blocks F3.2 implementation)
  - OQ-006: Overtime threshold value (affects F1.1, F2.3 implementation)
  - OQ-007: Leave/holiday source (affects F1.2, design decision)
  - OQ-008: Admin scope assignment mechanism (affects F4.1 initial setup)
  - OQ-009: Integration credential type & rotation (blocks F6.1 implementation)
  - OQ-012: Partial-week leave flag UI (affects F1.2 UX design)

### Consistency checks
- **Feature numbering:** F1.1–F1.3, F2.1–F2.4, F3.1–F3.2, F4.1–F4.3, F5.1–F5.2, F6.1. Numbering is consistent but not strictly sequential in count (Epic E4 has 3 features, E2 has 4, others have 2). This is acceptable.
- **Persona references:** All five personas are referenced in features (Team Member, Team Lead, Administrator, Financial Admin, System Admin). No orphaned personas; no features without a persona actor.
- **Epic-to-Feature alignment:**
  - E1 (Time Capture & Submission): F1.1, F1.2, F1.3 ✓
  - E2 (Multi-Level Approval Workflow): F2.1, F2.2, F2.3, F2.4 ✓
  - E3 (Financial Lock & Export): F3.1, F3.2 ✓
  - E4 (Administration & RBAC): F4.1, F4.2, F4.3 ✓
  - E5 (Reporting & Audit Trail): F5.1, F5.2 ✓
  - E6 (Integrations): F6.1 ✓

- **Terminology:** "timesheet", "time entry", "submission", "approval" are used consistently. "Locked" is used consistently for immutable state. Role terminology (Team Lead, Administrator, Financial Admin) matches user-prompt.

- **Scope alignment with user-prompt:**
  - User-prompt mentions 5 actors → spec has 5 personas ✓
  - User-prompt specifies status lifecycle (Draft → Submitted → Approved by Lead → Final Approved → Locked) → spec reflects this in features ✓
  - User-prompt specifies role-scoped visibility (Admin sees only assigned projects/users) → spec F4.3 implements this ✓
  - User-prompt specifies Financial Admin extra approval step → spec F2.4 implements this ✓
  - User-prompt specifies locked immutability → spec F3.1 implements this ✓
  - User-prompt specifies integration with DevOps/JIRA/Linear → spec F6.1 implements this ✓
  - User-prompt specifies reconciliation reporting → spec F5.1 implements this ✓
  - User-prompt specifies 7-year audit trail → spec F5.2 implements this with retention in §5.5 ✓

---

## Required fixes (if BLOCKED)

### Blocker 1: §5.11 Cost left as unconfirmed placeholder
**Status:** BLOCKS approval
**Issue:** §5.11 states "not yet confirmed — architect must present dev + prod cost estimate for Financial Admin sign-off". A placeholder or "to be estimated" cost is not acceptable; the spec must name a confirmed budget number.
**Fix:** Either:
  - (Option A) Financial Admin must confirm a fixed monthly Azure budget (e.g. "$5,000/month") and the spec rewrites §5.11 to state that number.
  - (Option B) The spec removes the placeholder and rewrites §5.11 to state "No budget cap confirmed; cost will be presented at architecture phase and must be approved before infra is deployed" — making it an explicit waiver, not a default.
  - The corresponding §7 OQ-001 entry must be updated to remove the `NFR default applied:` prefix if Option B is chosen.

---

## Notes (non-blocking)

1. **OQ-002 (compliance legal sign-off) is properly flagged** — POPIA and GDPR are named in §5.4, and the open question correctly asks for legal confirmation before architecture. This is not a blocker; it is a dependency for the Architect phase, not a Phase 1 responsibility.

2. **OQ-005 (export template format) blocks implementation** — F3.2 references "the billing template format" but does not specify it. This must be resolved before the Backend Developer phase, but is correctly listed in §7 as a design prerequisite. Not a blocker for spec quality, but a dependency to flag.

3. **OQ-006 (overtime threshold value) affects feature testability** — F1.1 and F2.3 flag entries with `Hours > 10`, but OQ-006 correctly asks whether this is a confirmed number or configurable. The current spec uses 10 as an example; OQ-006 should be confirmed before implementation, or the spec should state "confirmed value: 10 hours/day" if that is the decision. Currently acceptable as a documented assumption.

4. **OQ-010 (session policy) has a hidden assumption** — The spec states "idle timeout 30 minutes; absolute session timeout 8 hours" but marks this as a default for single concurrent session. If users access from multiple devices (desktop + mobile), the concurrent-session policy must be confirmed. Currently correctly flagged in OQ-010.

5. **Feature F2.3 (overtime validation) detail level** — The spec requires overtime entries to be validated with a ≥10-character justification note. This is precise and testable. No issue.

6. **Feature F4.3 (scoped admin visibility) is comprehensive** — The spec correctly specifies that Financial Admin is exempt from scope restrictions. This is a key design decision that prevents scope-check bypass. No issue.

7. **Audit trail design (F5.2) includes before/after JSON snapshots** — This is a strong choice for compliance; correctly specifies append-only immutability at the database constraint level (DELETE/UPDATE permissions revoked). No issue.

8. **Accessibility (§5.7) targets WCAG 2.1 AA** — Appropriate for an internal business tool. Keyboard-only and screen-reader support are required. No issue.

9. **Localization (§5.9) is English (South African) only at launch** — Appropriate for a domestic billing system. Future plans are none, which is acceptable. No issue.

10. **Feature count and complexity** — 17 features across 6 epics is a reasonable scope for a Phase 1 spec. No feature appears to be invented beyond the user-prompt; all align with stated goals.

11. **Acceptance criteria granularity** — Criteria are specific enough to guide development (e.g. F1.1 specifies exact validation messages, F3.1 specifies HTTP 403 behavior) without overspecifying implementation. This is good practice.

12. **Missing field in OQ-007 context** — The spec mentions "partial-week (leave/holiday) allowed without warning" in F1.2 acceptance criteria but does not provide a mechanism for the Team Member to flag a day as leave/holiday (noted in OQ-007 and OQ-012). This is correctly flagged as open; the interim approach (manual acknowledgement note) is documented in §6 Out of Scope ("leave/holiday partial-week flags are set manually").

---

## Summary

**Functional audit (§§1–4):** PASS — vision is clear, personas are named, epics are numbered and aligned, all 17 features use standard format with testable acceptance criteria, and all tasks are <= 1 dev-day.

**NFR audit (§5):** 10 of 11 sub-sections are fully testable with concrete numbers. §5.11 Cost is a **BLOCKER** — it is a placeholder stating "not yet confirmed" rather than a confirmed budget number.

**Compliance coherence:** PASS — POPIA and GDPR are correctly named based on feature triggers (billing data at scale, South African operation, EU exposure); correctly flagged for legal confirmation in OQ-002.

**Cross-cutting consistency:** PASS — section 6 is present with 7 out-of-scope items; section 7 lists 12 open questions with all 6 NFR defaults properly echoed with the `NFR default applied:` prefix; feature numbering, persona references, epic alignment, and terminology are all consistent; no scope creep beyond user-prompt.

---

VERDICT: BLOCKED
