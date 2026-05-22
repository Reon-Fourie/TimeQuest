# Frontend Critic — Iteration 1

## Build & test gate
- Build: PASS (0 errors, 0 warnings)
- Tests: PASS (22 passed, 0 failed, 0 skipped)

## Coverage
| Feature | Page/component | Status |
|---|---|---|
| F1.1 Daily Time Entry Logging | `Pages/Timesheet/Index.razor` — entries table, delete, flag display; `OpenEdit()` is a stub | PARTIAL — no usable time-entry form (modal deferred) |
| F1.2 Pre-Submission Entry Validation | `Pages/Timesheet/Index.razor` — missing-day warning banner shown; `SubmissionReviewModal` stub | PARTIAL — acknowledgement checkbox flow not implemented |
| F1.3 Weekly Timesheet Submission | `Pages/Timesheet/Index.razor` — submit button, success message, lock guard | PASS |
| F2.1 Team Lead Review Queue | `Pages/Approvals/Index.razor` | PASS |
| F2.2 Team Lead Approve / Reject | `Pages/Approvals/Detail.razor` — approve + reject with 10-char comment guard | PASS |
| F2.3 Overtime & After-Hours Validation | `Pages/Approvals/Detail.razor` — flagged entries displayed; per-entry justification input and approve-block not implemented | PARTIAL — deferred to iteration 2 per summary.md |
| F2.4 Financial Admin Extra Approval Step | `Pages/Financial/Queue.razor` + `Pages/Financial/QueueDetail.razor` | PASS |
| F3.1 Final Approval and Immutable Lock | `Pages/Financial/Lock.razor` — lock queue, ConfirmDialog, lock action | PARTIAL — row version passed as empty `[]`; see note |
| F3.2 Export Locked Timesheet Data | `Pages/Financial/Export.razor` — date filter, preview, BuildCsv(); JS interop download is a TODO stub | PARTIAL — no actual file download |
| F4.1 User and Team Management | `Pages/Admin/Teams/Index.razor` + `Pages/Admin/Teams/Detail.razor` — list, edit name/desc, remove members; create-team page absent | PARTIAL — no create-team form |
| F4.2 Project Creation and Assignment | `Pages/Admin/Projects/Index.razor` + `Pages/Admin/Projects/Detail.razor` — list, edit, deactivate, remove users; create-project page absent | PARTIAL — no create-project form |
| F4.3 Scoped Admin Visibility | No frontend component needed; enforced at service layer | N/A (service-layer concern) |
| F5.1 Reconciliation Reports | `Pages/Financial/Reports.razor` — type selector, date range, grouped results, empty state | PASS |
| F5.2 Comprehensive Audit Trail | `Pages/Audit/Index.razor` — search filters, keyset pagination, expandable before/after JSON | PASS |
| F6.1 Ticket Reference Validation | `Pages/SysAdmin/Integrations.razor` — health table, manual-approve action; list query is a stub | PARTIAL — pending service method; documented in summary.md |

## DTO reuse audit
- No shared DTOs redefined frontend-side: PASS
- Note: `Integrations.razor` defines a local `private record PendingEntry(int Id, TicketValidationStatus)` as a stub placeholder because `TicketValidationService` does not yet expose a list query. This is a component-local view model (different shape from `TimeEntryDto`), not a duplication of a shared DTO. Non-blocking; should be replaced with a proper shared DTO when the service method is added.

## UX findings
- Three-state coverage (loading/error/empty): PASS — all 14 page components implement all three states using `LoadingSkeleton`, `ErrorBanner`, and `EmptyState` shared components
- Auth protection: PASS — every protected page carries `[Authorize]` or `[Authorize(Roles = "...")]`; `Routes.razor` wraps the router in `CascadingAuthenticationState` with `AuthorizeRouteView`
- DbContext in components: none found — PASS
- Form validators (`EditForm` + `DataAnnotationsValidator`): FAIL — **all form pages (`Admin/Teams/Detail`, `Admin/Projects/Detail`, `Financial/Reports`, `Financial/Export`, `Audit/Index`) use native HTML `<form>` with `@onsubmit` and direct `@bind`; no `EditForm` or `DataAnnotationsValidator` is used anywhere in the component tree.** This means Blazor client-side validation (field-level error messages driven by data annotations on request DTOs) is not wired. Manual guards (e.g. `rejectComment.Length < 10` button disable) partially compensate but do not surface `ValidationMessage` components.

## Accessibility findings
- Skip-to-content link: present (`MainLayout.razor` line 3 — `<a class="skip-link" href="#main-content">`)
- Labels on form inputs: PASS — all form fields in `Detail` and filter-form pages have explicit `<label for="...">` wired to matching `id` attributes; `aria-required="true"` on required fields
- aria-live regions: present — rejection banner uses `aria-live="assertive"`, success message uses `aria-live="polite"`, error alerts use `role="alert"`

## Required fixes (if BLOCKED)
1. **Replace native `<form>` with `<EditForm>` + `<DataAnnotationsValidator>` + `<ValidationMessage>` on all form pages.** Affected files: `Admin/Teams/Detail.razor`, `Admin/Projects/Detail.razor`, `Financial/Reports.razor`, `Financial/Export.razor`, `Audit/Index.razor`. Bind to request-model instances (e.g. `UpdateTeamRequest`) rather than raw primitive fields. This is required by the Blazor implementation patterns and is necessary for consistent field-level validation messaging per F1.1 AC.

## Notes (non-blocking)
1. **`OpenEdit()` stub (F1.1)** — `TimeEntryFormModal` is not implemented; the edit button silently does nothing. Users cannot create or edit entries in this iteration. Documented gap in summary.md; must ship in iteration 2 before F1.1 can be signed off.
2. **`SubmissionReviewModal` stub (F1.2)** — The missing-day warning banner is rendered, but the acknowledgement-checkbox gating from F1.2 AC is absent. Submit is blocked only if `Entries.Count == 0`. Deferred to iteration 2.
3. **Overtime justification form (F2.3)** — Flagged entries are visible in `Approvals/Detail` but no per-entry validation note field is present, and the approve button is not blocked when flagged entries exist. Deferred to iteration 2.
4. **Lock row-version placeholder** — `Financial/Lock.razor` passes an empty `byte[]` to `LockTimesheetAsync`. This will cause optimistic-concurrency failures in production once the detail endpoint supplies a real `RowVersion`. Must be fixed before go-live.
5. **CSV download JS interop stub (F3.2)** — `BuildCsv()` runs but the file is never presented to the user. `DownloadCsv()` builds the CSV string then discards it (`_ = csv`). Iteration 2 must wire `IJSRuntime` interop.
6. **Create-team / create-project pages absent (F4.1, F4.2)** — Navigation links to `/admin/teams/new` and `/admin/projects/new` exist but the pages do not. Clicking them returns a 404. Must be implemented in iteration 2.
7. **`GetCurrentUserId()` returns 0 across all pages** — Entra OIDC ClaimsPrincipal wiring is deferred. All service calls that scope by user will behave incorrectly until this is resolved.
8. **Navigation links in pages use raw `<a>` tags instead of `<NavLink>`** — Back buttons and CTA links (`ApprovalQueueRow`, `EmptyState`, `Detail` back buttons) use plain `<a href=...>`. For non-nav-rail links this is acceptable in Blazor Server (Blazor's `<a>` tags still use client-side navigation via the router), but `<NavLink>` is preferred in components that are reusable. Low-risk, worth addressing for consistency.
9. **bUnit test coverage is zero** — No component tests exist. The integration test phase should include bUnit tests for at minimum: Timesheet/Index three-state rendering, Approvals/Detail approve/reject button guard, and Financial/Lock confirm dialog flow.
10. **Mobile nav drawer not wired** — The nav rail hides at `<768px` via CSS but there is no hamburger-menu trigger to show it. Mobile users have no navigation access. Should be addressed in iteration 2.

VERDICT: BLOCKED
