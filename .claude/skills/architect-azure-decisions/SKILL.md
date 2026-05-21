---
name: architect-azure-decisions
description: Azure service selection rubric used by the System Architect agent (phase 2). Maps architectural needs (compute, database, auth, secrets, jobs, storage, caching, observability, networking) to recommended services with rationale, plus a registry of common rejected alternatives. Invoke when picking services or justifying a choice in the design.md.
---

# Azure Decision Rubric

## Selection principle
Default to the **simpler** option. Pick a more complex service only when a specific spec requirement (functional feature or NFR sub-section) demands it. Always document the rationale in §3 (Rejected alternatives) of the design.md.

## Compute

| Need | Recommended | Pick this when | Don't pick (and why) |
|---|---|---|---|
| Stateful web app | App Service (Linux) | Default for Blazor Server | AKS - operational overhead; Container Apps - cold-start latency for interactive scenarios |
| Stateless API only | App Service or Functions | App Service for sustained traffic, Functions for spiky | AKS unless multi-team / polyglot |
| Event-driven workloads | Azure Functions (Consumption / Premium) | Spiky load, scale-to-zero acceptable | App Service - wastes idle capacity |
| Container microservices | Container Apps | True microservices, scale-to-zero needed | AKS only if K8s primitives required |
| Heavy customisation | AKS | Multi-tenant K8s, service mesh, daemon sets | App Service / ACA - usually enough |

## Database

| Need | Recommended | Pick this when | Don't pick |
|---|---|---|---|
| Relational, transactions, joins | Azure SQL | Default for line-of-business apps | Cosmos SQL API - higher cost, weaker joins |
| Global distribution + flexible schema | Cosmos DB | Multi-region writes, schema-on-read | SQL - manual sharding |
| Open-source SQL | Azure Database for PostgreSQL | Postgres in team's stack | Cosmos PG API - higher cost |
| Cache / session store | Azure Cache for Redis | Throughput needs in-memory store | SQL - too slow for hot keys |
| Search | Azure AI Search | Full-text + faceted search | SQL LIKE - inadequate at scale |
| Object storage | Azure Blob | Files, media, backups, large binaries | Storing bytes in SQL - inefficient |

## Authentication

| Need | Recommended | Pick this when | Don't pick |
|---|---|---|---|
| First-party app users (B2C) | Entra External ID | Customer-facing app, social logins, OIDC | Local ASP.NET Identity - DIY pain at scale; legacy Azure AD B2C - sunset path |
| Employee / partner identity | Entra ID | Enterprise SSO, conditional access | Identity - reinvents the wheel |
| Tiny MVP, local users | ASP.NET Core Identity | <100 users, no SSO needed, full control of UX | Entra External ID - overkill for prototype |

## Secrets & config

| Need | Recommended | Notes |
|---|---|---|
| Secrets | Azure Key Vault + Managed Identity | App Service reads via Key Vault references; never put connection strings in appsettings |
| Feature flags / dynamic config | App Configuration | Only if you have multi-env + dynamic-toggle needs; otherwise appsettings.json + env vars are enough |
| Application configuration | appsettings.{Env}.json + env vars + KV references | Default for any single-team app |

## Background work

| Need | Recommended | Notes |
|---|---|---|
| Periodic tasks (cron-like) | IHostedService in App Service | Default for low-volume, predictable schedules |
| Triggered work (queue, blob, timer) | Azure Functions | Default for event-driven |
| Long-running batch | Container Apps Jobs or Functions Durable | Picks depend on duration |
| Pub/sub fan-out | Service Bus + worker | Only when you actually have multiple subscribers; in-process events are usually fine |

## Observability

| Need | Recommended | Notes |
|---|---|---|
| APM + logs + metrics | Application Insights | Default. Wire via OpenTelemetry. Connection string from KV |
| Long-term log archive | Log Analytics workspace, then Storage export | Required if §5.6 retention > AI default (90 days) |
| Alerting | Azure Monitor alert rules + Action Groups | Alerts from §5.6 of spec |

## Networking

| Need | Recommended | Notes |
|---|---|---|
| Public endpoint, default | App Service with HTTPS-only | Default for dev/qa |
| Restrict by IP | App Service access restrictions | Cheap, simple. Use before VNet integration |
| Private only | Private Endpoints + VNet integration | Required if §5.4 specifies private network / regulated regime |
| Global load balancing | Front Door | Only when multi-region (§5.2) AND custom WAF needed |
| API gateway | APIM | Only when multiple downstream services, rate limiting per consumer, transformation needs |

## Deployment

| Need | Recommended | Notes |
|---|---|---|
| App deploy | GitHub Actions or Azure Pipelines | Either is fine; pick what team uses |
| Infra as code | Bicep | Default for Azure-only stacks |
| Multi-cloud IaC | Terraform | Only if mandated; Bicep is simpler for Azure-only |
| Container registry | Azure Container Registry | Shared across envs |

## Rejection patterns (copy into §3 when applicable)

These are the rejections you'll write most often. Each is 1 line max.

| Pattern | Standard reason |
|---|---|
| AKS over App Service | Operational overhead unjustified at this scale |
| Cosmos DB over Azure SQL | Spec needs joins / transactions; no global distribution requirement |
| APIM over direct App Service | Only one client / no rate-limit complexity yet |
| Front Door over single-region | No multi-region requirement in §5.2 |
| Service Bus over in-process events | Single producer/consumer, no decoupling benefit |
| App Configuration over appsettings | Single environment / no dynamic toggles |
| Functions over App Service for stateful web | Cold start incompatible with Blazor Server's persistent circuit |
| Entra External ID over local Identity | <100 users, no SSO, MVP scope |
| Local Identity over Entra External ID | App is customer-facing with SSO needs; rolling your own is risky |
| Container Apps over App Service | No scale-to-zero requirement; cold starts unhelpful for interactive UI |

## NFR -> service mappings (quick reference)

When the spec's §5 mandates X, the architecture must include Y:

| Spec NFR | Architectural implication |
|---|---|
| §5.1 p95 < 200ms | Distributed cache (Redis) probable, careful EF query plans |
| §5.2 multi-region | Front Door + paired-region App Service + Cosmos or SQL geo-replication |
| §5.3 SLA ≥ 99.95% | App Service Premium with availability zones, or multi-region |
| §5.3 RTO < 1h | Hot standby region OR daily-restored standby + tested runbook |
| §5.4 regulated regime (HIPAA/PCI/regulated-financial) | Private Endpoints + CMK + audit log export + restrict public network |
| §5.4 data residency = EU | All services in EU region; no global Cosmos write regions outside |
| §5.5 audit retention 7y | Log Analytics export to Storage with immutability policy |
| §5.6 tracing required | OpenTelemetry instrumentation + AI Distributed Tracing |
| §5.8 mobile native in scope | Add App Service Mobile API project or expose REST to Xamarin/MAUI |
| §5.11 tight budget | Use B-series SKUs, share Log Analytics, skip APIM/Front Door until needed |

## Cost discipline rules

- Every service in §2 has a cost estimate.
- Dev environment total stated explicitly at the bottom of §2.
- If total dev cost exceeds the budget from §5.11, the Critic blocks. Restructure (drop services, downgrade SKUs) until under.
- Prefer consumption / serverless SKUs for dev where they exist; always-on tiers only when required (e.g. Blazor Server needs always-on).
- Don't pay for prod-tier features in dev (no GRS storage, no Premium App Insights, no high-availability SQL).

## "No service salad" rule
If §2 lists more than 6 paid services for an MVP, the design is probably over-engineered. Re-read the spec and remove the ones that aren't tied to a feature or NFR.
