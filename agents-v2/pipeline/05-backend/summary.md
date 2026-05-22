# Backend Implementation — Iteration 2

## Files created/modified

### New projects scaffolded
- `src/TimeQuest.Domain/TimeQuest.Domain.csproj` (new)
- `src/TimeQuest.Infrastructure/TimeQuest.Infrastructure.csproj` (new)
- `src/TimeQuest.Shared/TimeQuest.Shared.csproj` (new)
- `tests/TimeQuest.UnitTests/TimeQuest.UnitTests.csproj` (new)

### Domain layer (TimeQuest.Domain)
- `src/TimeQuest.Domain/Enums.cs` (new) — 6 enums: UserTeamRole, BillingType, TimesheetStatus, ApprovalAction, TicketValidationStatus, DayFlagType
- `src/TimeQuest.Domain/Result.cs` (new) — Result<T> and Result value types
- `src/TimeQuest.Domain/Entities/ApplicationUser.cs` (new)
- `src/TimeQuest.Domain/Entities/Team.cs` (new)
- `src/TimeQuest.Domain/Entities/UserTeam.cs` (new)
- `src/TimeQuest.Domain/Entities/Project.cs` (new)
- `src/TimeQuest.Domain/Entities/ProjectUser.cs` (new)
- `src/TimeQuest.Domain/Entities/AdminProjectScope.cs` (new)
- `src/TimeQuest.Domain/Entities/WeeklyTimesheet.cs` (new)
- `src/TimeQuest.Domain/Entities/TimeEntry.cs` (new)
- `src/TimeQuest.Domain/Entities/TimesheetDayFlag.cs` (new) — composite PK (WeeklyTimesheetId, Date)
- `src/TimeQuest.Domain/Entities/ApprovalRecord.cs` (new)
- `src/TimeQuest.Domain/Entities/AuditEvent.cs` (new) — bigint PK, SetNull on Actor delete
- `src/TimeQuest.Domain/Interfaces/IAuditService.cs` (new)
- `src/TimeQuest.Domain/Interfaces/IScopeGuard.cs` (new)
- `src/TimeQuest.Domain/Exceptions/TimesheetNotFoundException.cs` (new)
- `src/TimeQuest.Domain/Exceptions/TimesheetAlreadyLockedException.cs` (new)
- `src/TimeQuest.Domain/Exceptions/TimesheetNotInExpectedStatusException.cs` (new)
- `src/TimeQuest.Domain/Exceptions/OvertimeNotValidatedException.cs` (new)

### Infrastructure layer (TimeQuest.Infrastructure)
- `src/TimeQuest.Infrastructure/Data/ApplicationDbContext.cs` (new) — IdentityDbContext<ApplicationUser, IdentityRole<int>, int>; all 11 entity sets; all FK delete behaviours; 14 indexes; 3 unique constraints
- `src/TimeQuest.Infrastructure/Data/SeedData.cs` (new) — idempotent runtime role seeder (not HasData — SQLite compatible)
- `src/TimeQuest.Infrastructure/Services/AuditService.cs` (new) — implements IAuditService
- `src/TimeQuest.Infrastructure/Services/ScopeGuard.cs` (new) — implements IScopeGuard
- `src/TimeQuest.Infrastructure/Services/TimeEntryService.cs` (new) — F1.1
- `src/TimeQuest.Infrastructure/Services/WeeklyTimesheetService.cs` (new) — F1.2, F1.3
- `src/TimeQuest.Infrastructure/Services/ApprovalService.cs` (new) — F2.1–F2.4, F3.1
- `src/TimeQuest.Infrastructure/Services/ExportService.cs` (new) — F3.2
- `src/TimeQuest.Infrastructure/Services/ReportService.cs` (new) — F5.1
- `src/TimeQuest.Infrastructure/Services/AdminService.cs` (new) — F4.1, F4.2, F4.3
- `src/TimeQuest.Infrastructure/Services/TicketValidationService.cs` (new) — F6.1 (stub adapters)
- `src/TimeQuest.Infrastructure/BackgroundServices/TicketValidationBackgroundService.cs` (new) — IHostedService 15-min retry

### Shared DTOs (TimeQuest.Shared)
- `src/TimeQuest.Shared/Dtos/TimeEntryDtos.cs` (new)
- `src/TimeQuest.Shared/Dtos/WeeklyTimesheetDtos.cs` (new)
- `src/TimeQuest.Shared/Dtos/ApprovalDtos.cs` (new)
- `src/TimeQuest.Shared/Dtos/AdminDtos.cs` (new)
- `src/TimeQuest.Shared/Dtos/ExportDtos.cs` (new)
- `src/TimeQuest.Shared/Dtos/ReportDtos.cs` (new) — includes ReportGroupBy enum
- `src/TimeQuest.Shared/Dtos/AuditDtos.cs` (new)

### Web project (TimeQuest)
- `TimeQuest/Program.cs` (modified)
- `TimeQuest/appsettings.json` (modified)

### Unit tests (TimeQuest.UnitTests)
- `tests/TimeQuest.UnitTests/TestHelpers/DbContextFactory.cs` (new)
- `tests/TimeQuest.UnitTests/TestHelpers/TestAuditService.cs` (new)
- `tests/TimeQuest.UnitTests/Services/WeeklyTimesheetServiceTests.cs` (new) — 6 tests
- `tests/TimeQuest.UnitTests/Services/ApprovalServiceTests.cs` (new) — 8 tests
- `tests/TimeQuest.UnitTests/Services/TimeEntryServiceTests.cs` (new) — 8 tests

---

## Build status

`dotnet build` -> 0 errors, 0 warnings

---

## Test results

`dotnet test` -> 22 passed, 0 failed, 0 skipped

---

## Feature coverage

| Feature | Service method | Endpoint | Tests |
|---|---|---|---|
| F1.1 Daily Time Entry | TimeEntryService.CreateEntryAsync / UpdateEntryAsync / DeleteEntryAsync | — | TimeEntryServiceTests (8 tests) |
| F1.2 Pre-Submission Validation | WeeklyTimesheetService.ValidateForSubmissionAsync / SetDayFlagAsync | — | WeeklyTimesheetServiceTests (4 tests) |
| F1.3 Submission | WeeklyTimesheetService.SubmitAsync / GetOrCreateForWeekAsync | — | WeeklyTimesheetServiceTests (2 tests) |
| F2.1 Lead Queue | ApprovalService.GetLeadQueueAsync / GetTimesheetDetailAsync | — | No unit tests (hot query; integration) |
| F2.2 Lead Approve/Reject | ApprovalService.LeadApproveAsync / LeadRejectAsync | — | ApprovalServiceTests (5 tests) |
| F2.3 Overtime Validation | ApprovalService.ValidateOvertimeAsync | — | ApprovalServiceTests (2 tests) |
| F2.4 Financial Approval | ApprovalService.FinancialApproveAsync / FinancialRejectAsync | — | ApprovalServiceTests (lock path) |
| F3.1 Final Lock | ApprovalService.LockTimesheetAsync | — | ApprovalServiceTests (2 tests) |
| F3.2 Export | ExportService.ExportLockedEntriesAsync | — | No unit tests (query-only; integration) |
| F4.1 Team Mgmt | AdminService.CreateTeamAsync / AssignTeamLeadAsync / AddTeamMemberAsync | — | No unit tests (scope: integration) |
| F4.2 Project Mgmt | AdminService.CreateProjectAsync / AssignUserToProjectAsync | — | No unit tests (scope: integration) |
| F4.3 Scope Guard | ScopeGuard.CanAccessProjectAsync / CanAccessUserAsync | — | No unit tests (scope: integration) |
| F5.1 Reports | ReportService.GetReconciliationReportAsync (Daily/Weekly/Monthly) | — | No unit tests (query-only; integration) |
| F5.2 Audit Trail | AuditService.LogAsync (wired to all mutating methods) | — | Indirect via TestAuditService |
| F6.1 Ticket Validation | TicketValidationService.ValidatePendingTicketsAsync / ApproveManuallyAsync | — | TimeEntryServiceTests (1 test — Pending status) |

---

## DTOs added to Shared project

- TimeEntryDto, CreateTimeEntryRequest, UpdateTimeEntryRequest
- WeeklyTimesheetDto, TimesheetDayFlagDto, SubmissionValidationResult
- ApprovalQueueItemDto, TimesheetDetailDto, ApprovalRecordDto, LeadRejectRequest, FinancialRejectRequest, ValidateOvertimeRequest, LockTimesheetRequest
- TeamDto, TeamMemberDto, CreateTeamRequest, ProjectDto, CreateProjectRequest, UserSummaryDto
- ExportRowDto
- ReconciliationReportDto, ReconciliationGroupDto, ProjectHoursSummary, ReportGroupBy
- AuditLogEntryDto, AuditSearchRequest

---

## NFR coverage

- §5.1 Performance: All approval queue queries use parameterised LINQ targeting the indexes from data design §3. decimal(5,2) configured via HasColumnType. FlagThresholdHours is config-driven, no magic constant.
- §5.4 Security: Connection string from IConfiguration only. UseAuthentication/UseAuthorization in pipeline. ScopeGuard at service layer. Notes, TicketRef, DisplayName values never logged. No FromSqlRaw with concatenation.
- §5.5 Auditing: Every mutating service method calls IAuditService.LogAsync (11 action types). AuditEvent.ActorId uses SetNull FK behaviour for GDPR erasure. Table is append-only (no Delete/Update in service code).
- §5.6 Observability: Production log level Warning; Information for auth events and all timesheet status transitions. Background service logs start/completion/failure. PII fields excluded from all ILogger calls.

---

## Changelog (iteration 2)
- Fix critic-1 #1: Added `IsLeadForTimesheetUserAsync` scope guard to `LeadApproveAsync` and `LeadRejectAsync` — returns `Result.Failure("Access denied.")` when lead is not in the timesheet user's team. Updated `ApprovalServiceTests.SeedSubmittedTimesheetAsync` to seed Team + UserTeam records.
- Fix critic-1 #2: Removed duplicate `AddScoped<AuditService>()` concrete registration from Program.cs; kept only `AddScoped<IAuditService, AuditService>()`.
- Fix critic-1 #3: Added `GetDisplayNameAsync` private helper to all services; all `LogAsync` calls now pass the actor's `DisplayName` from the database instead of `userId.ToString()`.
- Fix critic-1 #4: Added `string.IsNullOrWhiteSpace(request.Notes)` check at the top of `TimeEntryService.CreateEntryAsync` — returns `Result.Failure("Notes are required.")`.
- Fix critic-1 #5: Removed `ValidateOvertimeAsync` from `ApprovalService`; single authoritative implementation remains in `TimeEntryService`.

## Known gaps / deferred

- F5.2 audit search read path (AuditService.SearchAsync) not yet implemented — Frontend iteration will need it
- F6.1 real integration adapters (Azure DevOps/JIRA/Linear) are stubs — requires OQ-009 credential resolution
- EF Core migration not created (per task instructions — deferred to DB setup phase)
- Dev environment seed data (sample users, Dev Team, Project Alpha/Beta) deferred to deployment phase
- ScopeGuard, ExportService, ReportService unit tests deferred to IntegrationTests (better with DB fixture)
- TimeQuest.IntegrationTests project not yet created (placeholder only; no Testcontainers)
- AuditService double-registration in Program.cs (both concrete and interface) — should be cleaned to interface-only registration
