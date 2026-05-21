---
name: ba-spec-template
description: The canonical spec.md template structure used by the BA agent (phase 1) to write its output. Provides all 7 section headers, sub-headers under §5 NFRs, and inline guidance on what each section must contain. Invoke this skill ONLY when writing or rewriting the spec.md output - it is not needed during requirements elicitation.
---

# BA Spec Template

Use exactly these section headers. Do not invent new sections. Do not omit sections - if a section has no content, write "- (none)" so structure is preserved.

```markdown
# Spec: <Project Name>

## 1. Vision
<2-3 sentence elevator pitch. Not generic boilerplate. The reader should know what this product does and who it's for after one paragraph.>

## 2. User Personas
- **<Persona>** - <role, primary goal>
- ...

## 3. Epics
- E1: <epic title>
- E2: ...

## 4. Features
### F1.1: <Feature title> (Epic E1)
**As a** <persona> **I want** <capability> **so that** <outcome>.

**Acceptance criteria:**
- [ ] <testable criterion>
- [ ] ...

**Tasks:**
- T1.1.a: <implementation task - small enough to PR in a day>
- T1.1.b: ...

### F1.2: ...

## 5. Non-Functional Requirements

### 5.1 Performance
- **Server response time** (API): p50 < <X>ms, p95 < <X>ms, p99 < <X>ms
- **Page load (TTI)**: < <X>s on broadband
- **Throughput**: sustained <X> RPS; peak <X> RPS
- **Batch / background jobs**: max runtime, schedule window

### 5.2 Scalability
- **Concurrent users**: <X> launch; <Y> at 12 months
- **Data volume**: <rows or GB> launch; <projected> at 3 years
- **Geographic distribution**: single-region / multi-region / global

### 5.3 Availability & Reliability
- **Uptime SLA**: <99.0% / 99.9% / 99.95%>
- **Planned maintenance windows**: <yes/no, when>
- **Recovery objectives**: RTO = <duration>, RPO = <duration> (or "N/A")
- **Disaster recovery scope**: backup retention, restore-test cadence

### 5.4 Security & Compliance
- **Authentication mechanism**: <local Identity / Entra ID / Entra External ID / B2C>
- **Multi-factor**: <required / optional / not supported>
- **Data classification**: <Public / Internal / Confidential / Restricted> - list specific sensitive fields
- **Regulatory regimes**: <GDPR / POPIA / HIPAA / PCI-DSS / SOC 2 / COPPA / none>
- **Data residency**: <region(s)>
- **Encryption**: in-transit <TLS X.Y min>, at-rest <yes/no, key custody>
- **Session policy**: timeout <X>, idle timeout <X>, concurrent-session rules
- **Password policy** (if local auth): min length, complexity, rotation, history
- **Secret handling**: <Key Vault / GitHub Secrets / etc.>

### 5.5 Auditing
- **Audited actions**: <explicit list>
- **Audit record fields**: actor, action, target, before/after, timestamp UTC, correlation ID, source IP
- **Retention**: <N> years
- **Immutability**: append-only / tamper-evident / standard
- **Audit log readers**: <role(s)>

### 5.6 Observability
- **Production log level**: <Information / Warning>
- **Must be logged**: <list>
- **MUST NEVER be logged**: <redacted fields>
- **Log retention**: <N> days hot, <N> months cold
- **Required metrics**: <list, include 1-3 business KPIs>
- **Alerts**: <3-5 conditions that page>
- **Tracing**: required / not required

### 5.7 Accessibility
- **Target standard**: WCAG 2.1 <A / AA / AAA>
- **Keyboard-only navigation**: required
- **Screen-reader support**: required
- **Notes**: <user populations needing accommodations>

### 5.8 Browser & Device Support
- **Browsers**: <list with min versions>
- **Mobile**: <responsive yes/no; native iOS/Android in scope?>
- **Offline capability**: <required / progressive degradation / not required>
- **Minimum viewport width**: <Xpx>

### 5.9 Localisation & Internationalisation
- **Launch languages**: <list>
- **Future languages**: <list or "none planned">
- **Date/number/currency formats**: per-locale or fixed
- **Right-to-left support**: required / not required

### 5.10 Maintainability & Delivery
- **Code coverage minimum**: <X% on services, Y% overall>
- **Build time budget**: < <X> minutes
- **Deployment frequency target**: <e.g. multiple/week to dev, weekly to prod>
- **Documentation**: README + runbook required

### 5.11 Cost Constraints
- **Monthly Azure budget**: <amount> dev; <amount> prod
- **Per-user cost target**: <amount/user/month> at scale

## 6. Out of Scope
- <thing the user mentioned but we're explicitly NOT building>

## 7. Open Questions
- <questions the user couldn't answer yet - handed to architect / data designer>
- NFR default applied: §5.X - <value> (default). Confirm or override.
- ...
```

## Section-numbering invariants

- §1-§4: functional definition
- §5.1-§5.11: non-functional definition (the 11 sub-sections are immovable)
- §6: out of scope
- §7: open questions (including NFR defaults to confirm)

The BA critic gates on this exact structure. Renaming or renumbering will block.
