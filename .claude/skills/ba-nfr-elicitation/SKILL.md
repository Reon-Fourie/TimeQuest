---
name: ba-nfr-elicitation
description: Non-functional requirements elicitation rules, the 11 sub-section template, the sensible-defaults registry, the testability rubric, and the compliance trigger map. Invoke when you need to gather, default, or validate NFRs in a BA spec. Used by the BA agent (phase 1) and the BA critic (phase 1).
---

# NFR Elicitation Skill

## The 11 NFR sub-sections (§5 of the BA spec)

Every spec MUST include all 11. Never delete a sub-section because the user "didn't ask". Users rarely volunteer NFRs; you must elicit (or default + confirm).

### 5.1 Performance
- API response time: p50/p95/p99 in ms
- Page TTI on broadband (s)
- Throughput: sustained + peak RPS
- Batch job runtime caps + schedule window

### 5.2 Scalability
- Concurrent users at launch + 12mo
- Data volume at launch + 3yr (rows or GB)
- Geographic distribution: single-region / multi-region / global

### 5.3 Availability & Reliability
- Uptime SLA (%)
- Planned maintenance windows
- RTO + RPO (duration), or "N/A — no continuity requirement"
- DR scope: backup retention, restore-test cadence

### 5.4 Security & Compliance
- AuthN mechanism (local Identity / Entra ID / Entra External ID / B2C)
- MFA: required / optional / not supported
- Data classification of stored fields (call out sensitive ones)
- Regulatory regimes: GDPR / POPIA / HIPAA / PCI-DSS / SOC 2 / COPPA / none
- Data residency
- Encryption: in-transit, at-rest, key custody
- Session policy (timeout, idle, concurrent)
- Password policy (length, complexity, rotation, history)
- Secret handling (Key Vault / GitHub Secrets / etc.)

### 5.5 Auditing
- Audited actions (list specifically — don't write "everything")
- Audit record fields: actor, action, target, before/after, timestamp UTC, correlation ID, source IP
- Retention in years (or "N/A")
- Immutability: append-only / tamper-evident / standard table
- Who can read the audit log (roles)

### 5.6 Observability
- Production log level
- What MUST be logged (business events, errors with stack, slow queries)
- What MUST NEVER be logged (passwords, full PANs, tokens, full PII — list redacted fields)
- Log retention: N days hot, N months cold
- Required metrics: request rate, error rate, latency p50/p95, 1–3 key business KPIs
- 3–5 alerts that must page
- Tracing: required / not required

### 5.7 Accessibility
- Target: WCAG 2.1 A / AA / AAA
- Keyboard-only navigation
- Screen-reader support
- Any user populations needing specific accommodations

### 5.8 Browser & Device Support
- Browsers + min versions
- Mobile: responsive yes/no, native in scope?
- Offline: required / progressive degradation / not required
- Minimum viewport width

### 5.9 Localisation & Internationalisation
- Launch languages
- Future languages
- Date/number/currency: per-locale or fixed?
- Right-to-left support

### 5.10 Maintainability & Delivery
- Code coverage minimum
- Build time budget
- Deployment frequency target
- Documentation requirements

### 5.11 Cost Constraints
- Monthly Azure budget for dev (and prod if known)
- Per-user cost target (if SaaS)

## Sensible defaults registry

Use ONLY when the user says "you decide" / "I don't know" / "use sensible defaults". Apply the default, AND echo the same item into §7 Open Questions with prefix `NFR default applied:`.

| Sub-section | Default value |
|---|---|
| 5.1 Performance | API p95 < 500ms, p99 < 1s; page TTI < 2s on broadband; throughput sized to concurrent-users figure |
| 5.2 Scalability | If unstated: 50 concurrent users launch, 250 at 12mo; data volume estimated from features × scale; single-region |
| 5.3 Availability | 99.9% uptime; RTO/RPO N/A unless data loss is consequential |
| 5.4 Security baseline | Local ASP.NET Identity unless multi-tenant; MFA optional; TLS 1.2 min; encryption at rest on; Key Vault for secrets; 8-char min password; secrets never in code |
| 5.5 Auditing baseline | Log all auth events + all destructive writes + all permission changes; 1-year retention (non-regulated) or 7-year (regulated); append-only |
| 5.6 Observability | Structured JSON logs at Information level; redact passwords/tokens/full PII; 30 days hot, 12 months cold; metrics: request rate, error rate, p95 latency; alerts: error rate > 1% for 5min, p95 > target for 10min, deployment failure |
| 5.7 Accessibility | WCAG 2.1 AA; keyboard nav required; screen-reader support required |
| 5.8 Browsers | Evergreen: last 2 versions of Edge, Chrome, Firefox, Safari; responsive design; no offline; min viewport 360px |
| 5.9 Localisation | English only at launch; no future plans noted; fixed formats |
| 5.10 Maintainability | 70% coverage on services, 50% overall; build < 5 min; multiple deploys/week to dev, weekly to prod; README + runbook |
| 5.11 Cost | **NEVER default cost.** Always require an explicit number from the user. |

## Compliance trigger map

If feature text contains any of these triggers, the relevant regulatory regime MUST be named in §5.4 (and audit defaults MUST NOT be applied without user confirmation).

| Trigger in features | Regime to surface |
|---|---|
| patient, health record, clinical, medical, diagnosis, prescription | HIPAA (US) / equivalent local health-data law |
| card number, PAN, CVV, payment, checkout, billing | PCI-DSS |
| PII at scale, EU users, EEA residents, "the GDPR" | GDPR |
| South African users, ZA residents, "the POPI Act" | POPIA |
| children, minors, under-13, COPPA | COPPA |
| financial transaction at regulated scale, "FSCA", "MAS", "FCA", "SEC reporting" | Regulated-financial |
| government records, FOIA, classified | Public-sector / national |

When a trigger is detected, do NOT silently apply audit defaults — confirm retention and immutability with the user, and call out the specific obligations the regime mandates.

## Testability rubric

A value is testable when it is one of:
- A **number** with a unit (ms, RPS, %, GB, users, days, $)
- A **regime name** from a known set (WCAG 2.1 AA, TLS 1.2, GDPR, HIPAA, etc.)
- A **boolean / enum** with a defined set (yes/no, required/optional/none, single-region / multi-region / global)
- An **explicit "N/A"** with a one-line reason

If a sub-section's content can't be checked against an outcome in a test, code review, or audit script, it's not testable. Flag as a blocker.

## Elicitation protocol (for the BA in interactive mode)

1. Capture the functional spec first (§§1–4).
2. Run the compliance trigger map across the captured feature text. Note any triggers found.
3. **Either** invoke the `nfr-elicitor` sub-agent (Haiku) to draft a proposed §5 from features + personas + trigger findings, **or** if no sub-agent is available, walk the 11 sub-sections inline with the user.
4. Present the proposed §5 to the user as a strawman, with `[DEFAULT]` markers on inferred values.
5. Take user overrides for any markers they want to change.
6. Finalise §5 — values without overrides keep their defaults AND get echoed into §7 with `NFR default applied:` prefix.
7. For any compliance trigger detected, explicitly confirm the regime in §5.4 — never accept silence as "no regime".
8. Cost (§5.11) MUST be a user-confirmed number, never a default.

## Defaults echoing rule

For every NFR sub-section whose final value came from a default rather than user confirmation, append to §7 Open Questions:

```
- NFR default applied: §5.X — <value> (default). Confirm or override.
```

The BA critic blocks if any defaulted value is missing from §7.
