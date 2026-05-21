---
name: uiux-design-template
description: The canonical design.md template for the UI/UX Designer agent (phase 3). Provides all 10 section headers, sub-section structures (sitemap, journey, wireframe block, component inventory table, open questions), and section-numbering invariants. Invoke ONLY when writing or rewriting design.md - not needed while drafting wireframes or reviewing.
---

# UI/UX Design Template

Use exactly these section headers. The UI/UX Critic gates on this structure - renaming or renumbering blocks.

```markdown
# UI/UX Design - <Project Name>

## 1. Information Architecture

### Sitemap
\`\`\`
/                       (landing - anonymous)
/login
/dashboard              (auth required)
/<route>                (Role)
  /<route>/<sub>
...
\`\`\`

### Primary navigation
- Mechanism: left rail / top bar / hamburger - specify per breakpoint
- Visibility rules per role (which nav items show for which persona)
- Active-state indicator

## 2. Personas to journeys
One linear journey per persona, covering their key user stories.
- **<Persona>**: land -> login -> <step> -> <step> -> <outcome>
- **<Persona>**: ...

## 3. Screens (wireframes)
One sub-section per route. Each MUST include all of:
- **Purpose** (1 sentence)
- **Required role** (or "anonymous")
- **Render mode** (InteractiveServer / InteractiveAuto / Static) - call out only if not the project default
- **Key UI elements** (with placement)
- **Data shown** (DTO names if known, or "TBD pending data design")
- **Actions / CTAs**
- **Empty state** - copy + CTA when no data
- **Loading state** - skeleton / spinner / disabled buttons
- **Error state** - inline banner / page-level error / toast
- **Validation** (forms only) - per-field rules
- **Responsive notes** - what changes below the `md` breakpoint

Wireframe format: ASCII boxes are fine; Mermaid block diagrams are also acceptable. Don't mix styles within one screen.

Example structure (use as a model, not verbatim):
\`\`\`
### /orders (Customer)
Purpose: list the user's orders with quick access to "place new".
Required role: Customer
Render mode: InteractiveServer (project default)

+----------------------------------------+
| [App nav]                              |
| Your orders                  [+ New]   |
|----------------------------------------|
|  #1042  Placed 12 May   R 240  Open    |
|  #1041  Placed 09 May   R 510  Closed  |
+----------------------------------------+

Data: OrderListItemDto (Id, PlacedAt, Total, Status)
Empty state: title "No orders yet" + CTA "Place your first order" -> /orders/new
Loading: 5 skeleton rows
Error: inline banner with retry button
Validation: n/a (no form)
Responsive (< md): cards stack vertically, single column
\`\`\`

## 4. Component inventory
Reusable Blazor components the Frontend Dev should build. Group by purpose if list grows >10.
| Component | Purpose | Props |
|---|---|---|
| OrderCard | Display one order summary | OrderListItemDto |
| EmptyState | Generic empty placeholder | title, message, ctaLabel, ctaHref |
| LoadingSkeleton | N skeleton rows | rowCount |
| ErrorBanner | Inline error with retry | message, onRetry |

## 5. Design tokens
Reference the design-tokens block from the `uiux-design-system` skill. Override only the values the spec mandates differently (e.g. brand primary colour).

Document any overrides as a small CSS block under this header.

If dark mode is in scope (per spec or NFR §5.8), include `@media (prefers-color-scheme: dark)` overrides for color tokens only.

## 6. Responsive breakpoints
Reference the breakpoints from the `uiux-design-system` skill. Add only if the spec mandates a non-default value.

## 7. Accessibility commitments
Reference the WCAG AA baseline from the `uiux-design-system` skill. Add only project-specific accommodations beyond baseline.

## 8. Microcopy (key strings)
Dictionary of copy the Frontend Dev should use verbatim. Locale-friendly key names (dot-separated).
| Key | Copy |
|---|---|
| auth.login.title | "Sign in" |
| <feature>.empty.title | ... |
| common.error.retry | "Try again" |
| common.confirm.cancel | "Cancel" |
| common.confirm.delete | "Delete" |

## 9. Open questions for Data Designer
- Which entity fields drive each screen's data needs (so indexes can support them)
- Any list page where the natural sort or filter implies a specific index
- Any screen needing aggregation that's expensive without a denormalised field
- ...

## 10. Changelog (iteration deltas)
- Iteration 1: initial design
- Iteration 2 (if any): <what changed and why in response to critic-1.md>
```

## Section-numbering invariants
- §1: information architecture (sitemap + nav)
- §2: persona-to-journey mapping
- §3: per-route wireframes (the bulk)
- §4: component inventory
- §5-§8: design system + accessibility + microcopy
- §9: open questions for Data Designer
- §10: changelog (omit on iteration 1)

The UI/UX Critic gates on:
- All features in spec.md have a screen in §3 (or an explicit "no UI" note)
- All personas in spec.md have a journey in §2
- Every data-loading screen in §3 specifies empty/loading/error states
- Tokens, breakpoints, accessibility all reference (or override with reason) the `uiux-design-system` baseline
