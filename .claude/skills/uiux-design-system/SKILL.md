---
name: uiux-design-system
description: The baseline UI/UX design system used by the UI/UX Designer agent (phase 3). Holds default CSS design tokens (colors, typography, spacing, radius, shadow), responsive breakpoints, WCAG 2.1 AA accessibility checklist, microcopy conventions, and Blazor render-mode guidance. The designer references this skill verbatim in §5-§7 of design.md and only documents overrides. Invoke when filling §5/§6/§7 of design.md or when answering a critic finding about accessibility / tokens.
---

# UI/UX Design System (baseline)

The UI/UX Designer references this skill from §5, §6, §7 of design.md. Do not duplicate this content into design.md - reference it, and document only overrides.

## Default CSS design tokens

The Frontend Dev creates a single `tokens.css` (or `app.css`) with this content. Override only the values the spec mandates differently (e.g. brand primary).

```css
:root {
  /* Color - neutrals */
  --color-bg: #ffffff;
  --color-surface: #f5f5f7;
  --color-surface-elevated: #ffffff;
  --color-border: #e0e0e2;
  --color-text: #1a1a1a;
  --color-text-muted: #6a6a6e;
  --color-text-on-primary: #ffffff;

  /* Color - brand & semantic */
  --color-primary: #3b5bdb;
  --color-primary-hover: #2f4ac4;
  --color-primary-active: #243a9c;
  --color-success: #2f9e44;
  --color-warning: #e8590c;
  --color-danger: #c92a2a;
  --color-info: #1c7ed6;
  --color-focus-ring: #5c7cfa;

  /* Typography */
  --font-sans: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
  --font-mono: ui-monospace, "Cascadia Code", Consolas, monospace;
  --font-size-base: 16px;
  --font-size-sm: 14px;
  --font-size-xs: 12px;
  --font-size-lg: 18px;
  --font-size-xl: 22px;
  --font-size-2xl: 28px;
  --line-height-base: 1.5;
  --line-height-tight: 1.25;
  --font-weight-regular: 400;
  --font-weight-medium: 500;
  --font-weight-bold: 600;

  /* Spacing - 4px scale */
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-8: 32px;
  --space-10: 40px;
  --space-12: 48px;

  /* Radius */
  --radius-sm: 4px;
  --radius-md: 8px;
  --radius-lg: 12px;
  --radius-full: 9999px;

  /* Elevation */
  --shadow-sm: 0 1px 2px rgba(0,0,0,.06);
  --shadow-md: 0 2px 6px rgba(0,0,0,.08);
  --shadow-lg: 0 8px 24px rgba(0,0,0,.10);

  /* Motion - keep minimal */
  --motion-fast: 120ms;
  --motion-base: 180ms;
  --motion-ease: cubic-bezier(.2, .8, .2, 1);
}
```

### Dark mode (optional, only if spec demands)

```css
@media (prefers-color-scheme: dark) {
  :root {
    --color-bg: #0f1014;
    --color-surface: #1a1b21;
    --color-surface-elevated: #232430;
    --color-border: #2e2f3a;
    --color-text: #e8e8ea;
    --color-text-muted: #a0a0a6;
    --color-primary: #5c7cfa;
    --color-primary-hover: #748ffc;
  }
}
```

## Responsive breakpoints

| Name | Min width | Notes |
|---|---|---|
| sm | 0 | Single column, hamburger nav, full-width cards |
| md | 768px | Two columns common, persistent nav |
| lg | 1200px | Wider layout, optional side panels |
| xl | 1600px | Cap content width; centre with margins |

Designer rule: every screen in §3 must explicitly note what changes below the `md` breakpoint. "No change" is a valid answer for pure forms.

## Accessibility baseline (WCAG 2.1 AA)

The Frontend Dev must meet ALL of these. The UI/UX Critic blocks if §7 of design.md doesn't reference this baseline or omits any item that the spec mandates extra.

**Inputs and forms**
- All inputs have a visible `<label>`. Placeholder-only labels are NOT acceptable.
- Required fields are marked both visually and with `aria-required="true"`.
- Form errors are announced via `aria-describedby` pointing at the error message element.
- Validation errors appear inline next to the field AND in an `aria-live="polite"` summary on submit.

**Keyboard**
- Tab order matches visual order.
- All interactive elements reachable by keyboard (no mouse-only handlers).
- Focus is always visible (default browser outline OR a stronger `:focus-visible` ring using `--color-focus-ring`).
- Modal dialogs trap focus while open; restore focus to the trigger when closed.
- A skip-to-content link is the first focusable element on every page.

**Visual**
- Body text contrast >= 4.5:1; large text and UI controls >= 3:1.
- Color is never the only signal (use icon + label for status, not red/green alone).
- Don't disable user zoom. Respect `prefers-reduced-motion: reduce` for any animation.

**Semantics**
- Use native HTML semantics first: `<button>` not `<div onclick>`, `<nav>`, `<main>`, `<form>`, `<dialog>`.
- Headings (`<h1>`-`<h6>`) form a logical outline (no skipping levels for styling).
- Lists are `<ul>`/`<ol>`/`<dl>`, not styled divs.

**Async UX**
- Use `aria-live="polite"` for non-urgent status messages (saved, loaded).
- Use `aria-live="assertive"` only for genuine alerts (validation block, server error).
- Toasts that auto-dismiss must NOT auto-dismiss faster than 5s and must be dismissible by keyboard.

**Blazor-specific notes**
- `<NavLink>` provides accessible active-state styling - prefer it over raw `<a>` for in-app nav.
- `<EditForm>` integrates with DataAnnotationsValidator - wire `ValidationSummary` with `aria-live="polite"`.
- Render-mode discipline matters: `InteractiveServer` provides real keyboard accessibility; `Static` pages still need accessible markup but cannot do client-side validation.

## Microcopy conventions

The UI/UX Designer maintains the dictionary in §8 of design.md. Conventions:
- **Key format**: dot-separated, lowercase: `<area>.<element>.<state>`. Example: `orders.empty.title`.
- **Tone**: clear, neutral, professional. Avoid jokey copy unless the spec brand demands it.
- **Buttons**: verb-first, sentence case. "Save changes", "Place order", "Cancel".
- **Empty states**: short title + actionable CTA. Avoid sad-face emoji or excessive empathy ("Aww, nothing here").
- **Errors**: explain what went wrong AND what the user can do. Bad: "An error occurred." Good: "We couldn't load your orders. Check your connection and try again."
- **Validation**: per-field, specific. Bad: "Invalid". Good: "Enter a number between 1 and 99."

## Render-mode guidance for the designer

For each screen in §3, the default is `InteractiveServer` per the architect's choice. Mark the exception cases:

| Screen kind | Recommended render mode | Why |
|---|---|---|
| Landing / marketing | Static | SEO + fast first paint; no auth state needed |
| Login / register | InteractiveServer | Form interactivity; server-side validation |
| Auth'd app pages | InteractiveServer (default) | Default for the rest of the app |
| Real-time dashboards | InteractiveServer | Server push via SignalR circuit |
| Admin tools, low traffic | InteractiveServer | No SEO need; circuit cost negligible |
| Public-facing read-only | InteractiveAuto or Static | Avoid persistent circuit for anonymous traffic |

## Component-inventory conventions

Component names use PascalCase. Props are listed as DTO names where possible (to make backend/frontend contract explicit). Generic components (EmptyState, LoadingSkeleton, ErrorBanner, ConfirmDialog) appear once across the whole app - call them out so the Frontend Dev knows to build them generically rather than per-feature.

## Defaulting rule

If the spec is silent on a token / breakpoint / accessibility item, use the baseline above. Document the default decision in §5/§6/§7 by writing: "Baseline from `uiux-design-system` skill (no overrides)." Do not paste the full token block into design.md.
