# TimeQuest — Design Spec
**Date:** 2026-05-21  
**Stack:** Blazor Web App · ASP.NET Core Identity · EF Core 9 · SQL Server · .NET 10

---

## Overview

TimeQuest is a timesheet web app with RPG game mechanics. Employees log hours against projects, managers approve timesheets, and approvals award XP that drives level progression and badge unlocks. Managers oversee team members. Admins manage the full org.

---

## Architecture

Single Blazor Web App project. Tightly coupled, no micro-service split. Structure:

```
TimeQuest/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Models/
│   ├── ApplicationUser.cs
│   ├── Team.cs
│   ├── Project.cs
│   ├── UserTeam.cs
│   ├── UserProject.cs
│   ├── TimeEntry.cs
│   ├── Badge.cs
│   ├── UserBadge.cs
│   └── Enums.cs
├── Repositories/
│   ├── IRepository.cs
│   ├── Repository.cs
│   ├── ITimeEntryRepository.cs
│   ├── TimeEntryRepository.cs
│   ├── IUserRepository.cs
│   ├── UserRepository.cs
│   ├── ITeamRepository.cs
│   ├── TeamRepository.cs
│   ├── IProjectRepository.cs
│   ├── ProjectRepository.cs
│   ├── IBadgeRepository.cs
│   └── BadgeRepository.cs
├── Services/
│   ├── TimesheetService.cs
│   ├── XpService.cs
│   ├── BadgeService.cs
│   ├── ReportingService.cs
│   └── TeamService.cs
├── Helpers/
│   └── LevelHelper.cs
└── Components/          ← UI (phase 2)
```

**Data flow:** Blazor components → Services → Repositories → EF Core → SQL Server

---

## Roles

Three ASP.NET Core Identity roles seeded at startup:

| Role | Capabilities |
|---|---|
| `Employee` | Log time, view own stats, badges, history |
| `Manager` | Everything Employee can do + approve/reject timesheets for team members, view team reports |
| `Admin` | Full access — manage users, teams, projects, org-wide reports, approve any timesheet |

Manager approval rights are scoped: a Manager can only approve entries for users who are `Member` of a team where that Manager has `Manager` role in `UserTeam`.

---

## Data Model

### ApplicationUser (extends IdentityUser)
| Field | Type | Notes |
|---|---|---|
| FirstName | string | |
| LastName | string | |
| XP | int | Total lifetime XP |
| Level | int | Derived from XP, stored for query performance |
| AvatarUrl | string? | Optional profile pic |
| CreatedAt | DateTime | |

### Team
| Field | Type |
|---|---|
| Id | int |
| Name | string |
| Description | string? |
| CreatedAt | DateTime |

### Project
| Field | Type | Notes |
|---|---|---|
| Id | int | |
| Name | string | |
| Description | string? | |
| IsActive | bool | Soft disable |
| CreatedAt | DateTime | |

### UserTeam (join)
| Field | Type | Notes |
|---|---|---|
| UserId | string | FK → ApplicationUser |
| TeamId | int | FK → Team |
| Role | TeamRole | Member / Manager |

### UserProject (join)
| Field | Type |
|---|---|
| UserId | string |
| ProjectId | int |

### TimeEntry
| Field | Type | Notes |
|---|---|---|
| Id | int | |
| UserId | string | FK → ApplicationUser |
| ProjectId | int | FK → Project |
| Date | DateOnly | The date work was done |
| Hours | decimal | Precision: 18,2 |
| Description | string | What was worked on |
| Status | TimeEntryStatus | Draft / Submitted / Approved / Rejected |
| SubmittedAt | DateTime? | Set when employee submits |
| ReviewedAt | DateTime? | Set when manager acts |
| ReviewedById | string? | FK → ApplicationUser (manager) |
| RejectionReason | string? | Populated on rejection |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |

### Badge
| Field | Type | Notes |
|---|---|---|
| Id | int | |
| Name | string | |
| Description | string | |
| ImagePath | string | Relative path under wwwroot/badges/ |
| Criteria | BadgeCriteria | Enum — one value per badge type |

### UserBadge (join)
| Field | Type |
|---|---|
| UserId | string |
| BadgeId | int |
| EarnedAt | DateTime |

---

## Enums

```csharp
public enum TeamRole { Member, Manager }

public enum TimeEntryStatus { Draft, Submitted, Approved, Rejected }

public enum BadgeCriteria
{
    FirstEntry,          // First approved time entry
    Hours100,            // 100 total approved hours
    Hours1000,           // 1,000 total approved hours
    FourWeekStreak,      // 4 consecutive weeks with ≥1 approved entry
    SpeedRunner,         // Entry approved within 24h of submission
    TeamPlayer,          // Approved hours on 3+ different projects
    WeeklyOverachiever   // 40+ approved hours in a single calendar week
}
```

---

## XP & Leveling

**Rate:** 1 approved hour = 10 XP. XP is awarded when a Manager or Admin approves a `TimeEntry`.

**Level table** (hardcoded in `LevelHelper`):

| Level | XP Required | Title |
|---|---|---|
| 1 | 0 | Novice |
| 2 | 100 | Apprentice |
| 3 | 300 | Journeyman |
| 4 | 600 | Veteran |
| 5 | 1,000 | Expert |
| 6 | 1,500 | Master |
| 7 | 2,100 | Legend |
| 8 | 2,800 | Mythic |
| 9 | 3,600 | Immortal |
| 10 | 4,500 | TimeQuest Champion |

`LevelHelper.GetLevel(int xp)` returns the current level. `LevelHelper.GetXpForNextLevel(int xp)` returns XP needed to reach the next level.

---

## Approval Workflow

```
Employee creates entry     → Status: Draft
Employee submits entry     → Status: Submitted, SubmittedAt = now
Manager/Admin approves     → Status: Approved, ReviewedAt = now, ReviewedById = manager
                              → XpService.AwardXp(userId, hours)
                              → BadgeService.CheckAndAwardBadges(userId)
Manager/Admin rejects      → Status: Rejected, ReviewedAt = now, RejectionReason = reason
                              → Employee can edit and resubmit (back to Draft → Submitted)
```

Managers see a queue of `Submitted` entries for their team members. Admins see all `Submitted` entries.

---

## Predefined Badges

Images stored in `wwwroot/badges/`. Seeded to DB at startup.

| Name | Criteria | Description |
|---|---|---|
| First Quest | FirstEntry | Logged and got your first approved timesheet |
| Centurion | Hours100 | 100 total approved hours |
| Legendary | Hours1000 | 1,000 total approved hours |
| On a Roll | FourWeekStreak | 4 consecutive weeks with approved time |
| Speed Runner | SpeedRunner | Entry approved within 24 hours of submission |
| Team Player | TeamPlayer | Approved hours across 3+ different projects |
| Overachiever | WeeklyOverachiever | 40+ approved hours in a single week |

---

## Services

| Service | Responsibilities |
|---|---|
| `TimesheetService` | Create, submit, approve, reject time entries |
| `XpService` | Award XP on approval, recalculate and persist level |
| `BadgeService` | Check all badge criteria after XP award, grant new badges |
| `TeamService` | Create/manage teams, assign users, assign managers |
| `ReportingService` | User stats, team report, admin org report |

---

## Repositories

| Repository | Key methods |
|---|---|
| `TimeEntryRepository` | `GetByUser`, `GetPendingForManager`, `GetByTeam`, `GetByDateRange` |
| `UserRepository` | `GetWithBadges`, `GetLeaderboard`, `GetTeamMembers` |
| `TeamRepository` | `GetWithMembers`, `GetManagedBy` |
| `ProjectRepository` | `GetActive`, `GetByUser` |
| `BadgeRepository` | `GetAll`, `GetEarnedByUser`, `GetNotEarnedByUser` |

All repositories inherit from a generic `Repository<T>` base that wraps `ApplicationDbContext`.

---

## Reporting

| Report | Audience | Contents |
|---|---|---|
| My Stats | Employee | XP, level, badge shelf, hours by project, entry history |
| Team Report | Manager | Hours per member, top XP earner, hours by project, pending approvals count |
| Org Report | Admin | All teams summary, total org hours, leaderboard, badge distribution |

---

## Seed Data

On startup:
1. Seed Identity roles: `Admin`, `Manager`, `Employee`
2. Seed default Admin user (configurable via `appsettings.json`)
3. Seed all 7 badges
4. Seed sample teams/projects (dev mode only)
