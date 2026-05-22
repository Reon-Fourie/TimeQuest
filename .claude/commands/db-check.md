Help the user verify the TimeQuest database state after an operation. Ask for the operation performed and the relevant record(s) if not provided, then guide through the verification checklist.

IMPORTANT RULES — enforce these strictly, no exceptions:
- NEVER run INSERT, UPDATE, or DELETE statements against any database table. Read-only SELECT queries only.
- NEVER suggest or offer to modify, add, or remove any data in the database.
- If the user asks you to change data, remind them this skill is read-only and they must do it manually or via seed code.
- Environment is Local Dev unless the user specifies otherwise.
- Always ask for the linked feature / issue unless the user explicitly says "no link".

TimeQuest schema reference (EF Core 9, default `dbo` schema unless overridden):
- ApplicationUser (Id, Email, Xp, Level, …) — extends Identity user
- TimeEntry (Id, UserId, Date, Hours, Description, Status [Submitted/Approved/Rejected], ApprovedByUserId, ApprovedAt)
- Team (Id, Name)
- UserTeam (UserId, TeamId, TeamRole [Manager/Member])
- Badge (Id, Code, Name, Description, IconPath, …)
- BadgeAward (Id, UserId, BadgeId, AwardedAt)
- AspNetUsers / AspNetRoles / AspNetUserRoles (Identity tables; roles seeded: Admin, Manager, Employee)


**DB Check — Post Operation Verification**

**Operation Performed**
- Short description of what was done (e.g. Manager approved TimeEntry, badge awarded on level-up)

**Environment**
Local Dev

**Record Identifier**
- Table + ID used to locate the record (e.g. TimeEntry.Id = 47, ApplicationUser.Email = …)

**Linked Feature / Issue**
- [Feature name / GitHub issue #]


**Fields to Verify**

| Field | Expected Value | Actual Value | Pass/Fail |
|-------|---------------|--------------|-----------|
|       |               |              |           |


**Approval-Specific Checks** *(if applicable)*
- [ ] `TimeEntry.Status` = `Approved`
- [ ] `TimeEntry.ApprovedByUserId` = correct Manager or Admin
- [ ] `TimeEntry.ApprovedAt` set within expected range
- [ ] Approver is in same Team as entry's user (via UserTeam, TeamRole = Manager) OR is an Admin
- [ ] `ApplicationUser.Xp` incremented by Hours × 10
- [ ] `ApplicationUser.Level` recalculated to match new Xp
- [ ] New BadgeAward rows created for any newly-earned badges (and only those)

**Audit / History Checks**
- [ ] `CreatedBy` / `UpdatedBy` (or audit columns) reflect the correct user
- [ ] Timestamps are accurate

**Related Records**
- [ ] No unintended records created or modified
- [ ] Foreign key relationships intact
- [ ] Cascading effects verified (if applicable)

**Notes / SQL Used**
- Paste any SELECT queries run and their results here

> This skill is read-only. No data will be inserted, updated, or deleted during a DB check.
