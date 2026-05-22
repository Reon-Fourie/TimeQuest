# Frontend Implementation — Iteration 1

## Components created/modified

### Layout
- `TimeQuest/Components/Layout/MainLayout.razor` (modified) — skip-link, layout wrapper, main content landmark
- `TimeQuest/Components/Layout/NavMenu.razor` (modified) — role-based nav rail using AuthorizeView per role

### Infrastructure
- `TimeQuest/Components/_Imports.razor` (modified) — added Authorization, Shared.Dtos, Domain, Infrastructure.Services namespaces
- `TimeQuest/Components/Routes.razor` (modified) — CascadingAuthenticationState + AuthorizeRouteView
- `TimeQuest/Components/App.razor` (modified) — link to tokens.css
- `TimeQuest/Program.cs` (modified) — AddCascadingAuthenticationState()
- `TimeQuest/wwwroot/tokens.css` (new) — full design-token CSS file

### Shared components
- `TimeQuest/Components/Shared/EmptyState.razor` (new)
- `TimeQuest/Components/Shared/LoadingSkeleton.razor` (new)
- `TimeQuest/Components/Shared/ErrorBanner.razor` (new)
- `TimeQuest/Components/Shared/StatusChip.razor` (new)
- `TimeQuest/Components/Shared/ConfirmDialog.razor` (new)
- `TimeQuest/Components/Shared/ApprovalQueueRow.razor` (new)

### Pages
- `TimeQuest/Components/Pages/Timesheet/Index.razor` (new)
- `TimeQuest/Components/Pages/Approvals/Index.razor` (new)
- `TimeQuest/Components/Pages/Approvals/Detail.razor` (new)
- `TimeQuest/Components/Pages/Financial/Queue.razor` (new)
- `TimeQuest/Components/Pages/Financial/QueueDetail.razor` (new)
- `TimeQuest/Components/Pages/Financial/Lock.razor` (new)
- `TimeQuest/Components/Pages/Financial/Reports.razor` (new)
- `TimeQuest/Components/Pages/Financial/Export.razor` (new)
- `TimeQuest/Components/Pages/Admin/Teams/Index.razor` (new)
- `TimeQuest/Components/Pages/Admin/Teams/Detail.razor` (new)
- `TimeQuest/Components/Pages/Admin/Projects/Index.razor` (new)
- `TimeQuest/Components/Pages/Admin/Projects/Detail.razor` (new)
- `TimeQuest/Components/Pages/Audit/Index.razor` (new)
- `TimeQuest/Components/Pages/SysAdmin/Integrations.razor` (new)
- `TimeQuest/Components/Pages/AccessDenied.razor` (new)
- `TimeQuest/Components/Pages/NotFound.razor` (modified)

### Backend additions required by Frontend
- `src/TimeQuest.Infrastructure/Services/AdminService.cs` (modified) — added GetTeamsAsync, GetTeamDetailAsync, UpdateTeamAsync, DeleteTeamAsync, GetProjectsAsync, GetProjectDetailAsync, UpdateProjectAsync, RemoveUserFromProjectAsync, GetAllUsersAsync
- `src/TimeQuest.Infrastructure/Services/AuditService.cs` (modified) — added SearchAsync (keyset-paginated audit log read)
- `src/TimeQuest.Shared/Dtos/AdminDtos.cs` (modified) — added TeamDetailDto, ProjectDetailDto, ProjectUserDto, ProjectSummaryDto, UpdateTeamRequest, UpdateProjectRequest
- `src/TimeQuest.Shared/Dtos/AuditDtos.cs` (modified) — added AuditLogSearchResultDto

---

## DTOs reused (from backend Shared project)

Project reference: TimeQuest -> TimeQuest.Shared

- WeeklyTimesheetDto, TimesheetDayFlagDto, SubmissionValidationResult
- TimeEntryDto
- ApprovalQueueItemDto, TimesheetDetailDto, ApprovalRecordDto
- ReconciliationReportDto, ReconciliationGroupDto, ProjectHoursSummary, ReportGroupBy
- ExportRowDto
- TeamDto, TeamDetailDto, TeamMemberDto, UpdateTeamRequest
- ProjectSummaryDto, ProjectDetailDto, ProjectUserDto, UpdateProjectRequest
- AuditLogEntryDto, AuditSearchRequest, AuditLogSearchResultDto
- UserSummaryDto (available for pickers)

No DTOs were duplicated frontend-side.

---

## Routes

| Route | Page component | Required role | Render mode |
|---|---|---|---|
| /timesheet | Pages/Timesheet/Index | All authenticated | InteractiveServer |
| /timesheet/{weekStart} | Pages/Timesheet/Index | All authenticated | InteractiveServer |
| /approvals | Pages/Approvals/Index | TeamLead | InteractiveServer |
| /approvals/{timesheetId} | Pages/Approvals/Detail | TeamLead | InteractiveServer |
| /financial/queue | Pages/Financial/Queue | FinancialAdmin | InteractiveServer |
| /financial/queue/{id} | Pages/Financial/QueueDetail | FinancialAdmin | InteractiveServer |
| /financial/lock | Pages/Financial/Lock | FinancialAdmin | InteractiveServer |
| /financial/reports | Pages/Financial/Reports | FinancialAdmin | InteractiveServer |
| /financial/export | Pages/Financial/Export | FinancialAdmin | InteractiveServer |
| /admin/teams | Pages/Admin/Teams/Index | Administrator | InteractiveServer |
| /admin/teams/{teamId} | Pages/Admin/Teams/Detail | Administrator | InteractiveServer |
| /admin/projects | Pages/Admin/Projects/Index | Administrator | InteractiveServer |
| /admin/projects/{projectId} | Pages/Admin/Projects/Detail | Administrator | InteractiveServer |
| /audit | Pages/Audit/Index | FinancialAdmin, SystemAdmin | InteractiveServer |
| /sysadmin/integrations | Pages/SysAdmin/Integrations | SystemAdmin | InteractiveServer |
| /403 /access-denied | Pages/AccessDenied | Anonymous | Static |
| /404 /not-found | Pages/NotFound | Anonymous | Static |

Note: /login handled by Entra OIDC — no app-owned UI per spec.

---

## Build status

dotnet build -> 0 errors, 0 warnings

---

## Test results

dotnet test -> 22 passed, 0 failed, 0 skipped (existing service unit tests; no bUnit tests this iteration)

---

## State coverage

| Component | Empty | Loading | Error |
|---|---|---|---|
| Timesheet/Index | yes | yes | yes |
| Approvals/Index | yes | yes | yes |
| Approvals/Detail | yes | yes | yes |
| Financial/Queue | yes | yes | yes |
| Financial/QueueDetail | yes | yes | yes |
| Financial/Lock | yes | yes | yes |
| Financial/Reports | yes | yes | yes |
| Financial/Export | yes | yes | yes |
| Admin/Teams/Index | yes | yes | yes |
| Admin/Teams/Detail | yes (members) | yes | yes |
| Admin/Projects/Index | yes | yes | yes |
| Admin/Projects/Detail | yes (users) | yes | yes |
| Audit/Index | yes | yes | yes |
| SysAdmin/Integrations | yes | yes | yes |

---

## Accessibility checks

- Labels on all form inputs: PASS
- aria-required on required fields: PASS
- aria-live on async status: rejection banner (assertive), error banners (assertive), success message (polite)
- Skip-to-content link: present in MainLayout.razor -> #main-content
- Focus visible: confirmed via :focus-visible in tokens.css
- Table scoped headers: scope="col" on all th elements
- Action button aria-labels include row context (e.g. "Review timesheet for Alice Johnson, week 12/05/2025")
- StatusChip: text label always present, not colour-only
- ConfirmDialog: aria-modal="true", aria-labelledby + aria-describedby wired

---

## Microcopy

Used dictionary from UI/UX design.md §8. No deviations — all empty-state titles, rejection banner, submit success string, approve/reject labels match §8 exactly.

---

## Known gaps

1. TimeEntryFormModal + SubmissionReviewModal — not implemented; OpenEdit() is a stub. Deferred to iteration 2.
2. /admin/teams/new and /admin/projects/new create-form pages — links present, pages deferred to iteration 2.
3. GetCurrentUserId() returns 0 — ClaimsPrincipal wiring deferred until Entra OIDC is configured.
4. CSV file download in Financial/Export — BuildCsv() implemented; JS interop trigger is a TODO stub.
5. Lock row version — Financial/Lock passes empty byte[] to LockTimesheetAsync; detail endpoint needed to supply real value.
6. TicketValidationService list query — SysAdmin/Integrations has stub empty list; GetPendingValidationsAsync not yet in service.
7. bUnit tests — no bUnit package reference in solution; deferred to integration test phase.
8. Responsive mobile hamburger nav — nav hides at <768px via CSS; drawer not yet wired.
9. Flagged entry overtime justification form on Approvals/Detail — wireframe section deferred to iteration 2.
