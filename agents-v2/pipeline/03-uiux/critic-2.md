# UI/UX Critic — Iteration 2

## Feature coverage
| Feature | Screen / route | Status |
|---|---|---|
| F1.1 Daily Time Entry Logging | `/timesheet/{weekStart}` + Add/Edit Entry Modal; duplicate-entry guard noted as no-UI in §1 | PASS |
| F1.2 Pre-Submission Entry Validation | `SubmissionReviewModal` described in §2 Team Member journey and §4 component inventory | PASS |
| F1.3 Weekly Timesheet Submission | `/timesheet/{weekStart}` submit button + `SubmissionReviewModal`; status chip update shown; confirmation microcopy `timesheet.week.submit.success` present in §8 | PASS |
| F2.1 Team Lead Review Queue | `/approvals` | PASS |
| F2.2 Team Lead Approve / Reject | `/approvals/{timesheetId}`; rejection comment surfaced on Team Member's `/timesheet/{weekStart}` via REJECTED STATE + `WeeklyTimesheetDto` fields | PASS |
| F2.3 Overtime & After-Hours Validation | No-UI note in §1; flagged-entry validation section in `/approvals/{timesheetId}` with corrected min-10-chars rule | PASS |
| F2.4 Financial Admin Extra Approval | `/financial/queue` + `/financial/queue/{timesheetId}` | PASS |
| F3.1 Final Approval and Immutable Lock | `/financial/lock` | PASS |
| F3.2 Export Locked Timesheet Data | `/financial/export` | PASS |
| F4.1 User and Team Management | `/admin/teams` + `/admin/teams/{teamId}` | PASS |
| F4.2 Project Creation and Assignment | `/admin/projects` + `/admin/projects/{projectId}` | PASS |
| F4.3 Scoped Admin Visibility | No-UI note in §1 (service-layer enforcement; manifests as filtered lists and HTTP 403) | PASS |
| F5.1 Reconciliation Reports | `/financial/reports` | PASS |
| F5.2 Comprehensive Audit Trail | `/audit` | PASS |
| F6.1 Ticket Reference Validation | `/sysadmin/integrations` + `TicketRefValidator` within Add/Edit Entry Modal | PASS |

All 15 features accounted for. No invented features detected.

---

## Persona journeys
- Team Member: PASS — Full journey covers login → `/timesheet/{currentWeek}` → add entry → submit via `SubmissionReviewModal` → confirmation; now also includes rejected-timesheet return path: amber banner with rejection comment/rejector/date, entries editable again, resubmit. Both required paths (happy path F1.3 + rejection return path F2.2) are present.
- Team Lead: PASS — Full journey covers login → `/approvals` → drill to `/approvals/{timesheetId}` → validate overtime → approve or reject with mandatory comment.
- Financial Admin: PASS — Full journey covers login → `/financial/queue` → approve/reject → `/financial/lock` → `/financial/reports` → `/financial/export`.
- Administrator: PASS — Full journey covers login → `/timesheet` → Teams (create, assign members) → Projects (create, assign users).
- System Admin: PASS — Full journey covers login → `/sysadmin/integrations` → health status → pending/failed validations → approve/retry.

---

## State coverage audit
| Screen | Empty | Loading | Error |
|---|---|---|---|
| `/timesheet/{weekStart}` | yes | yes | yes |
| `/timesheet/{weekStart}` — REJECTED STATE | yes (amber banner + editable entries) | n/a | n/a |
| Add/Edit Entry Modal (validation) | N/A | N/A | yes (VALIDATION ERROR STATE shown) |
| `/approvals` | yes | yes | yes |
| `/approvals/{timesheetId}` | yes | yes | yes |
| `/financial/queue` | yes | yes | yes |
| `/financial/queue/{timesheetId}` | yes ("No entries in this timesheet") | yes | yes |
| `/financial/lock` | yes | yes | yes |
| `/financial/reports` (results area) | yes | yes | yes |
| `/financial/export` (preview area) | yes | yes | yes |
| `/admin/teams` | yes | yes | yes |
| `/admin/teams/{teamId}` (member list) | yes ("No members yet — click '+ Add members'…") | yes | yes |
| `/admin/projects` | yes | yes | yes |
| `/admin/projects/{projectId}` (user list) | yes ("No users assigned yet — click '+ Add users'…") | yes | yes |
| `/audit` | yes | yes | yes |
| `/sysadmin/integrations` (validations) | yes | yes | yes |

All data-loading screens now have all three states. Full PASS.

---

## Prior blocker resolution

1. **Overtime justification min-chars**: RESOLVED — §3 `/approvals/{timesheetId}` Validation section now reads "**Justification (per flagged entry)**: required to unlock 'Validate ✓'; min 10 chars (matches spec F2.3)". Changelog entry confirms the fix.

2. **/financial/queue/{id} empty state**: RESOLVED — §3 `/financial/queue/{timesheetId}` now includes `EMPTY | No entries in this timesheet`. Confirmed in screen spec and §10 changelog.

3. **/admin/teams/{id} member empty state**: RESOLVED — §3 `/admin/teams/{teamId}` now shows `EMPTY (members) | No members yet — click "+ Add members" to assign people to this team`. Confirmed present.

4. **/admin/projects/{id} user empty state**: RESOLVED — §3 `/admin/projects/{projectId}` now shows `EMPTY (users) | No users assigned yet — click "+ Add users" to assign people to this project`. Confirmed present.

5. **Rejection banner + DTO + journey**: RESOLVED — `/timesheet/{weekStart}` has a full REJECTED STATE wireframe with amber banner, rejection comment, rejector name/date. `WeeklyTimesheetDto` now includes `RejectionComment?`, `RejectedByName?`, `RejectedAt?` with rendering logic documented. Team Member journey updated with explicit rejection return path. Microcopy keys `timesheet.week.rejected.banner` and `timesheet.week.rejected.by` added in §8.

6. **Submission confirmation microcopy**: RESOLVED — `timesheet.week.submit.success` key added to §8 with copy "Timesheet for week of {date} submitted for approval". Changelog confirms addition.

All 6 prior blockers resolved.

---

## Accessibility audit
- Labels: PASS — §7 retains `aria-label` with row context on all approval table action buttons; nav items carry `aria-current="page"`. Rejection banner and new REJECTED STATE do not introduce unlabelled interactive elements.
- Focus visible: PASS — §7 references WCAG 2.1 AA baseline (`:focus-visible` styling required); modal open/close focus cycles documented.
- Keyboard nav: PASS — §7 references WCAG AA baseline; all core flows (time entry, submission, approval, lock, export) keyboard-operable; Escape key closes all modals.
- Colour independence: PASS — §7 explicitly states `StatusChip` must include a text label and optionally an icon with `aria-hidden="true"` and must not rely on colour alone. Amber rejection banner relies on text ("Your timesheet was returned for correction") not colour alone.
- ARIA live (async): PASS — `TicketRefValidator` status updates mandated via `aria-live="polite"` in §7; `ValidationSummary` component carries `aria-live` per §4 component inventory.
- Modal focus trap: PASS — §7 requires `<dialog>` with `aria-modal="true"`, focus trapped on open, focus returned to trigger on close, Escape closes, for `TimeEntryFormModal`, `SubmissionReviewModal`, and `ConfirmDialog`.

---

## Token consistency
- Hardcoded colours outside tokens: none — all colour references in §5 use `var(--color-*)` aliases resolving through design-system baseline tokens. The new rejection banner relies on the amber/warning semantic token (`--color-status-flagged` or warning class from the baseline); no hex values appear in screen specs, component definitions, or the new REJECTED STATE block.

---

## New issues found in iteration 2

### New Finding 1 — F2.4 rejection path: `WeeklyTimesheetDto` covers both rejection stages, but this is not explicit (Non-blocking)
Critic-1 Note 6 flagged that spec F2.4 requires the Financial Admin rejection comment also to be visible to the submitter. The design now surfaces `RejectionComment`/`RejectedByName`/`RejectedAt` on `WeeklyTimesheetDto` (§3, Data section for `/timesheet/{weekStart}`), and §9 Open Question 10 confirms these fields come from "the most recent rejection record for the timesheet". This implicitly covers both lead-rejection and financial-rejection paths, since both write to `ApprovalRecord`. However, the design does not state explicitly that the `RejectionComment` surfaces whichever is the most recent rejection regardless of which approver made it. The Data Designer's OQ-10 partially covers this but a brief clarifying sentence in the `/timesheet/{weekStart}` data note would remove any implementation ambiguity. This is non-blocking.

### New Finding 2 — `/financial/queue/{timesheetId}` rejection comment microcopy (Non-blocking)
The financial approval detail screen (`/financial/queue/{timesheetId}`) requires a mandatory rejection comment (min 10 chars; Reject button disabled). A hint below the comment field ("minimum 10 characters") is specified on the approval detail screen (`approvals.reject.hint` key exists). However, the financial detail screen does not reference a dedicated microcopy key for its rejection hint. The `approvals.reject.hint` key could be reused, but dedicated keys `financial.reject.placeholder` and `financial.reject.hint` would allow copy differentiation in future (raised in Critic-1 Note 5; still present). Non-blocking.

### New Finding 3 — `[+ Create team]` / `[+ Create project]` modal flow still undocumented (Non-blocking)
Critic-1 Notes 3 and 4 flagged that neither the create-team nor create-project flow is explicitly wired to a modal or navigation target in §3. Both flows still only reference the list page wireframes with a `[+ Create team]` / `[+ Create project]` button and no description of whether this opens a modal with `TeamForm`/`ProjectForm` or navigates to a new route. This is a non-blocking implementation ambiguity, unchanged from iteration 1.

---

## Required fixes (if BLOCKED)
None. All 6 prior blockers are resolved and no new blocking issues were found.

---

## Notes (non-blocking)
1. **F2.4 rejection comment — implicit dual-stage coverage:** The `WeeklyTimesheetDto` `RejectionComment` field implicitly covers both lead-rejection and financial-rejection but the design does not state this explicitly. A one-sentence clarification in the `/timesheet/{weekStart}` data note (e.g. "sourced from the most recent `ApprovalRecord` rejection entry regardless of stage") would remove developer ambiguity. (Carried from Critic-1 Note 6 — now partially addressed by OQ-10 in §9, still worth a tighter statement in §3.)
2. **Financial rejection microcopy keys:** Consider adding `financial.reject.placeholder` and `financial.reject.hint` keys to §8 to allow future copy differentiation from the team-lead rejection path. (Carried from Critic-1 Note 5.)
3. **Create-team / create-project modal flow undocumented:** `[+ Create team]` and `[+ Create project]` buttons remain unlinked to a defined modal or route in §3. A brief note clarifying these open `TeamForm`/`ProjectForm` in create mode would eliminate implementation uncertainty. (Carried from Critic-1 Notes 3 & 4.)
4. **Add/Edit Entry Modal edit-path error state:** When the modal opens in edit mode, there is no defined error state for a failed pre-fill fetch. In practice data is already loaded in the parent circuit, but a defensive inline `ErrorBanner` within the modal would improve resilience. (Carried from Critic-1 Note 1, unchanged.)
5. **`/financial/reports` vs `/financial/export` disambiguation:** The design still does not explicitly distinguish the "Export CSV" on `/financial/reports` (period-scoped) from the `/financial/export` page (arbitrary date range). A brief clarifying note would help developers avoid duplicate or conflicting export paths. (Carried from Critic-1 Note 2, unchanged.)

VERDICT: APPROVED
