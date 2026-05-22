# Test Cases — Timesheet & Billing Workflow System (Harvest Replacement)

**Spec Version:** 1.0 (2026-05-21)
**Prepared By:** LydiaB
**Date:** 2026-05-22
**Coverage:** Exhaustive — happy path, negative, edge, cross-role denial across all §7 AC sections.

**Shared Preconditions (assumed for all cases unless stated otherwise):**
- Seeded users: 1 Team Member (`tm1@test.local`), 1 second Team Member in same team (`tm2@test.local`), 1 Team Lead (`lead1@test.local`) over Team A, 1 Admin scoped to Project P1 only (`admin1@test.local`), 1 Financial Admin (`fa@test.local`).
- Seeded projects: P1 (Team A assigned), P2 (Team B assigned, NOT in Admin scope).
- Expected weekly hours threshold: 40h (Mon–Fri, 8h/day).
- Current week: 2026-05-18 → 2026-05-22.

---

## 1. Time Logging (ST-001)

AC bullets:
- a) Entry must include: Project, Hours, Notes
- b) Missing required field prevents save
- c) Hours logged > expected hours flags for review
- d) Duplicate entry detected → prevented or flagged

---

### TC-001 — Log valid time entry (happy path)
**AC:** ST-001 (a)
**Preconditions:** Logged in as `tm1@test.local`. P1 assigned to user.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Time Capture page | Page loads with empty entry form |
| 2 | Select Project = P1, Hours = 8, Notes = "Sprint planning", Date = today | Form accepts all values |
| 3 | Click Save | Entry persists; appears in today's list; status = Draft |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-002 — Missing Project field is blocked
**AC:** ST-001 (b)
**Preconditions:** Logged in as `tm1@test.local`.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open new time entry form | Form loads |
| 2 | Leave Project blank; enter Hours=4, Notes="x" | No validation error yet |
| 3 | Click Save | Save blocked; inline error "Project is required" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-003 — Missing Hours field is blocked
**AC:** ST-001 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open new entry form | Form loads |
| 2 | Select Project=P1, leave Hours blank, Notes="x" | No error yet |
| 3 | Click Save | Save blocked; inline error "Hours is required" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-004 — Missing Notes field is blocked
**AC:** ST-001 (b) (Notes marked Required in §8)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open new entry form | Form loads |
| 2 | Select Project=P1, Hours=8, leave Notes blank | No error yet |
| 3 | Click Save | Save blocked; inline error "Notes is required" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-005 — Hours = 0 rejected (edge)
**AC:** ST-001 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open entry form | Form loads |
| 2 | Project=P1, Hours=0, Notes="x", Date=today | Form shows error or save blocks |
| 3 | Click Save | Validation: "Hours must be greater than 0" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-006 — Negative Hours rejected (edge)
**AC:** ST-001 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open entry form | Form loads |
| 2 | Project=P1, Hours=-2, Notes="x" | Inline validation |
| 3 | Click Save | Save blocked; "Hours must be a positive number" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-007 — Hours > 24 in a single day rejected (edge)
**AC:** ST-001 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open entry form | Form loads |
| 2 | Project=P1, Hours=25, Notes="x", Date=today | — |
| 3 | Click Save | Save blocked; "Hours cannot exceed 24 per day" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-008 — Hours > expected threshold flagged (does not block)
**AC:** ST-001 (c), §9
**Preconditions:** `tm1@test.local` already has 8h logged today on P1.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Add a second entry: P2, Hours=6, Notes="overtime evening", Date=today | Form accepts |
| 2 | Click Save | Entry saves with status flag "Flagged for review" / over-threshold badge |
| 3 | Confirm entry visible in list | Entry shown with warning indicator; not blocked |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-009 — Duplicate entry (same Date + Project) blocked or flagged
**AC:** ST-001 (d), §9
**Preconditions:** Existing entry: Date=today, Project=P1, Hours=4.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Attempt new entry: Date=today, Project=P1, Hours=2, Notes="extra" | — |
| 2 | Click Save | Either save blocks with "Duplicate entry exists" OR saves with "Duplicate flagged" warning |
| 3 | Inspect persisted state | Matches whichever rule is enforced; no silent duplicate |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-010 — Same Date different Project is NOT a duplicate (negative-negative)
**AC:** ST-001 (d)
**Preconditions:** Existing entry: today, P1, 4h.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | New entry: Date=today, Project=P2, Hours=4, Notes="other project" | — |
| 2 | Click Save | Saves successfully — different project is not duplicate |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-011 — Optional Task Type and Ticket Reference accepted
**AC:** ST-001 (a), §8 (optional fields)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open entry form | Form loads |
| 2 | Fill required + Task Type="Development", Ticket Ref="JIRA-123" | Form accepts |
| 3 | Click Save; reopen entry | Optional fields persisted |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-012 — Cannot log time against project user is not assigned to
**AC:** ST-001 (a), §4 RBAC
**Preconditions:** `tm1@test.local` assigned to P1 only.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open entry form | Project dropdown lists only P1 |
| 2 | Attempt to submit P2 via direct API call / inspect element override | Server rejects with 403 / validation error |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 2. Weekly Submission (ST-002, ST-003)

AC bullets:
- a) Incomplete week → system warns before allowing submission
- b) Valid complete week → submission succeeds, enters approval workflow
- c) Partial week (leave/holiday) → allows adjusted hours

---

### TC-013 — Submit complete week (happy path)
**AC:** ST-002 (b)
**Preconditions:** `tm1@test.local` has 8h logged each weekday Mon–Fri this week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Weekly Timesheet | Week summary shows 40h total, all days populated |
| 2 | Click Submit | Confirmation dialog appears |
| 3 | Confirm | Status changes Draft → Submitted; entry enters Lead's approval queue |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-014 — Incomplete week shows warning
**AC:** ST-002 (a), §9
**Preconditions:** `tm1@test.local` has time for Mon/Tue/Wed only this week (Thu/Fri missing).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Weekly Timesheet | Week shows 24h, Thu/Fri highlighted as missing |
| 2 | Click Submit | Warning dialog: "Missing entries for 2 days. Continue?" |
| 3 | Choose Cancel | Submission aborted; status remains Draft |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-015 — Incomplete week submission proceeds after acknowledgement
**AC:** ST-002 (a), §9 (user must acknowledge)
**Preconditions:** Same as TC-014.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Weekly Timesheet → Submit | Warning dialog displays |
| 2 | Acknowledge / Continue | Submission succeeds; status = Submitted |
| 3 | Check audit log | Acknowledgement is recorded with timestamp |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-016 — Partial week with leave allows reduced hours without warning
**AC:** ST-003 (c), §9
**Preconditions:** `tm1@test.local` marked on leave Thu/Fri (leave entries logged); Mon–Wed have 8h each.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Weekly Timesheet | Thu/Fri show "Leave" status, not "Missing" |
| 2 | Click Submit | NO missing-days warning shown |
| 3 | Confirm | Status = Submitted; week total = 24h work + 16h leave |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-017 — Cannot resubmit an already-Submitted week
**AC:** ST-002 (b), §5 lifecycle
**Preconditions:** Week already in Submitted state.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Weekly Timesheet | Submit button disabled / hidden |
| 2 | Attempt resubmit via API | 409 Conflict or "Already submitted" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-018 — Cannot edit entries after submission (until rejected)
**AC:** ST-002 (b), §5
**Preconditions:** Week in Submitted state.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open a daily entry within submitted week | Edit fields disabled / read-only |
| 2 | Attempt to save change via API | 403 Forbidden / validation error |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-019 — Empty week submission blocked
**AC:** ST-002 (a) — edge
**Preconditions:** `tm1@test.local` has zero entries this week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Weekly Timesheet → Submit | Either submission blocked with "No entries to submit" OR warning requires explicit override |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-020 — Pre-submission warning lists which days are missing (validation review)
**AC:** ST-003 — pre-submission warning detail
**Preconditions:** Missing Tue, Thu.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Submit | Warning lists "Tuesday, Thursday" by name/date, not just a count |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-021 — Cross-role: Admin cannot submit another user's timesheet
**AC:** §4 RBAC
**Preconditions:** Logged in as `admin1@test.local`. tm1 has a Draft week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to tm1's weekly timesheet | View-only (no Submit control) |
| 2 | Attempt submit via API on tm1's week | 403 Forbidden |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 3. Team Lead Approval (ST-007, ST-008, ST-009)

AC bullets:
- a) Lead approves → status updates to "Approved by Lead"
- b) Lead rejects → status reverts to "Draft" with comments visible to submitter
- c) Overtime entries → Lead must explicitly validate before approval

---

### TC-022 — Lead approves a Submitted timesheet (happy path)
**AC:** ST-007/008 (a)
**Preconditions:** Logged in as `lead1@test.local`. tm1's week is Submitted; no overtime; tm1 in Team A.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Approvals queue | tm1's week appears |
| 2 | Open tm1's week and click Approve | Confirmation dialog |
| 3 | Confirm | Status = "Approved by Lead"; entry moves to Financial Admin queue |
| 4 | Check audit | Approval record created (Approver=lead1, Status=Approved, Timestamp set) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-023 — Lead rejects with comments
**AC:** ST-008 (b), §8 (Comments required on rejection)
**Preconditions:** Lead viewing tm1's Submitted week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Reject | Comment field appears, marked required |
| 2 | Leave comment blank, click Confirm | Blocked: "Comments required for rejection" |
| 3 | Enter "Friday hours look wrong" and Confirm | Status reverts to Draft; comment stored |
| 4 | Log in as tm1 → open week | Rejection comment visible on dashboard / week view |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-024 — Rejected week becomes editable again
**AC:** ST-008 (b)
**Preconditions:** Lead has just rejected tm1's week (from TC-023).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Log in as tm1, edit Friday entry | Edit allowed; saves |
| 2 | Resubmit week | Status → Submitted; returns to Lead queue |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-025 — Overtime requires explicit validation toggle
**AC:** ST-009 (c)
**Preconditions:** tm1's week has Friday=12h (overtime). Status=Submitted.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Lead opens tm1's week | Friday flagged as overtime |
| 2 | Click Approve without checking "Validate overtime" | Blocked: "Overtime entries must be validated" |
| 3 | Check "Overtime validated" then Approve | Approval succeeds; status = Approved by Lead; audit captures who validated |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-026 — Lead cannot approve their own timesheet (self-approval guard)
**AC:** §4 RBAC implication
**Preconditions:** lead1 has own Submitted week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | lead1 navigates to own week in Approvals view | No Approve button visible, or button disabled |
| 2 | Attempt self-approve via API | 403 Forbidden |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-027 — Lead cannot approve users outside their team
**AC:** §4 RBAC
**Preconditions:** lead1 leads Team A only. A Submitted week exists for a Team B member.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | lead1 opens Approvals queue | Only Team A members listed |
| 2 | Force-navigate to Team B member's week URL | 403 or "Not authorised" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-028 — Lead cannot reject Draft (not yet submitted) week
**AC:** §5 lifecycle
**Preconditions:** tm1's week is in Draft (never submitted).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | lead1 attempts to open it from Approvals queue | Week not in queue (filtered to Submitted) |
| 2 | Force-load URL | Approve/Reject controls hidden or disabled |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-029 — Team Member cannot approve own week
**AC:** §4 RBAC
**Preconditions:** tm1 has Submitted week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | tm1 logs in and looks for Approvals page | Approvals link not visible |
| 2 | Force-navigate /approvals | Redirect to Forbidden / 403 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-030 — Audit record on approval includes actor, timestamp, before/after status
**AC:** §12
**Preconditions:** lead1 just approved tm1's week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Query audit log for the week | Row exists: Actor=lead1, Action=Approved, Timestamp=ISO 8601, From=Submitted, To=Approved by Lead |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 4. Financial Admin Approval & Lock (ST-010, ST-011)

AC bullets:
- a) Lead-approved → Herman/Deon extra approval step triggered
- b) Financial Admin final approval → locks immediately
- c) Locked → no edits by any role

---

### TC-031 — Lead-approved week appears in Financial Admin extra-approval queue
**AC:** ST-010 (a)
**Preconditions:** lead1 has just approved tm1's week.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Log in as `fa@test.local` | Financial Admin dashboard |
| 2 | Open Extra Approvals queue | tm1's week is listed with status "Approved by Lead" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-032 — Financial Admin applies extra approval step
**AC:** ST-010 (a)
**Preconditions:** As TC-031.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open tm1's week | Extra Approve and Final Approve actions visible |
| 2 | Click "Extra Approve (Herman/Deon)" | Status advances; final approval now enabled |
| 3 | Check audit | Two-step approval visible: extra approval recorded before final |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-033 — Final approval locks the timesheet immediately
**AC:** ST-011 (b)
**Preconditions:** tm1's week extra-approved by fa.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Final Approve | Status = Locked immediately |
| 2 | Inspect entries | All entries marked Locked; no Edit/Delete UI |
| 3 | Check audit | Lock event recorded with Actor=fa, Timestamp |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-034 — Locked entry cannot be edited by Team Member
**AC:** ST-011 (c)
**Preconditions:** tm1's week Locked.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Log in as tm1 → open week | Entries read-only |
| 2 | Attempt edit via direct API | 403 / "Timesheet is locked" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-035 — Locked entry cannot be edited by Team Lead
**AC:** ST-011 (c)
**Preconditions:** As TC-034.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | lead1 attempts to edit any entry in the locked week | Blocked; "Timesheet is locked" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-036 — Locked entry cannot be edited by Admin (scoped)
**AC:** ST-011 (c)
**Preconditions:** Week in P1 (in admin1's scope), Locked.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 attempts to edit any entry | Blocked even though within scope |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-037 — Locked entry cannot be edited even by Financial Admin
**AC:** ST-011 (c) — "no edits by any role"
**Preconditions:** As TC-034.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa attempts to edit a locked entry | Blocked with same message |
| 2 | Confirm via DB if available | No update is possible through application paths |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-038 — Financial Admin cannot finalize without extra approval step first
**AC:** ST-010 (a) — sequencing
**Preconditions:** Week is "Approved by Lead", no extra approval done.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa opens week; clicks Final Approve directly | Blocked: "Extra approval step required first" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-039 — Financial Admin can reject at extra-approval step
**AC:** §5 lifecycle implication
**Preconditions:** Week "Approved by Lead".
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa clicks Reject at extra-approval stage; enters comment "Project code wrong" | Status reverts (Draft or back to Lead); comment visible |
| 2 | tm1 sees rejection with fa's comment | — |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-040 — Cross-role: Team Lead cannot final-approve
**AC:** §4 RBAC
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | lead1 logs in; navigates to Final Approval | Page not accessible / 403 |
| 2 | Attempt final approve via API | 403 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-041 — Cross-role: Admin cannot final-approve
**AC:** §4 RBAC
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 attempts to access Final Approval queue | 403 / not visible |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 5. Scoped Admin Access (ST-012, ST-013)

AC bullets:
- a) Admin views only assigned people/projects
- b) Admin edits/removes only assets within assigned scope

---

### TC-042 — Admin sees only assigned projects
**AC:** ST-012 (a)
**Preconditions:** admin1 assigned to P1 only.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Log in as admin1; open Projects list | Only P1 visible; P2 not in list |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-043 — Admin sees only people in assigned projects
**AC:** ST-012 (a)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 opens Users list | Only users assigned to P1 listed; Team B users hidden |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-044 — Admin cannot access out-of-scope project by URL
**AC:** ST-012 (a)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 navigates directly to /projects/P2 | 403 Forbidden or "Not authorised" page |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-045 — Admin cannot access out-of-scope user by URL
**AC:** ST-012 (a)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 navigates to a Team B user profile | 403 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-046 — Admin can edit in-scope project assignments (happy)
**AC:** ST-013 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 opens P1 → assignments | Editable |
| 2 | Add tm2 to P1 | Save succeeds |
| 3 | Check audit | Change recorded with Actor=admin1 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-047 — Admin cannot edit out-of-scope project via API
**AC:** ST-013 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 calls PUT /projects/P2 with body change | 403 Forbidden; no DB change |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-048 — Admin cannot remove user from out-of-scope team
**AC:** ST-013 (b)
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | admin1 calls DELETE on Team B user assignment | 403 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-049 — Cross-role: Team Member cannot access Admin pages
**AC:** §4
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | tm1 navigates to /admin/users | 403 / redirected |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-050 — Financial Admin has full visibility (positive cross-check)
**AC:** §4 key constraints
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa opens Projects list | Both P1 and P2 visible |
| 2 | fa opens Users list | All users across teams visible |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 6. Export (ST-014)

AC bullets:
- a) Export includes only Locked entries
- b) Export format must match the billing template format

---

### TC-051 — Export contains only Locked entries (happy path)
**AC:** ST-014 (a)
**Preconditions:** Test data has weeks in each state — Draft, Submitted, Approved by Lead, Locked.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa opens Export → selects date range covering all states | Preview/count shown |
| 2 | Run Export | Output file contains only entries from Locked weeks; other statuses excluded |
| 3 | Verify row counts vs expected | Count matches Locked entries in range |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-052 — Export format matches billing template (columns, order, types)
**AC:** ST-014 (b)
**Preconditions:** Billing template reference available.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Run Export | File produced |
| 2 | Open file; compare headers to template | Column names and order match exactly |
| 3 | Sample 5 rows: date formats, hours decimal places, currency | All match template spec |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-053 — Empty result (no Locked entries in range) — graceful
**AC:** ST-014 (a) — edge
**Preconditions:** Selected date range has no Locked weeks.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Run Export | Either empty file with headers only OR friendly "No data" notice; no crash |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-054 — Locked entries from multiple users in different teams all included
**AC:** ST-014 (a), §4 (FA full visibility)
**Preconditions:** Locked weeks exist for tm1 (Team A) and tm2 (Team B).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa exports the full range | Both users' Locked entries present |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-055 — Edits made after lock are NOT reflected in export (immutability check)
**AC:** ST-011 (c) + ST-014 (a)
**Preconditions:** A Locked week. Attempt to manipulate DB if test infra allows OR rely on attempted-edit blocked.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Confirm pre-lock values | Recorded |
| 2 | Run export now | Values match locked snapshot |
| 3 | If any backend edit path exists, attempt update; re-export | Re-export shows same locked values (or update path is blocked entirely) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-056 — Cross-role: Non-Financial-Admin cannot trigger export
**AC:** §4
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | lead1 navigates to Export page | Not visible / 403 |
| 2 | admin1 navigates to Export page | Not visible / 403 |
| 3 | tm1 navigates to Export page | Not visible / 403 |
| 4 | Attempt POST /export via API as each above | All return 403 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-057 — Export action is audited
**AC:** §12
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | fa runs an export | Audit log entry: Actor=fa, Action=Export, Timestamp, range filter parameters captured |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 7. Cross-Cutting Audit (§12) — covered above in TC-015, TC-023, TC-030, TC-033, TC-046, TC-057

## 8. Out of scope for this suite
- Integrations (ST-015) — DevOps/JIRA/Linear ticket validation: requires live integration; covered as a separate harness once endpoints exist.
- Reporting (ST-016) — daily/weekly/monthly reconciliation report generation: deferred until report formats finalized.
- Future ST-018 resource planning: explicitly out of scope per §2.

---

**Total cases:** 57
**Coverage:** §7 Acceptance Criteria (ST-001, 002, 003, 007, 008, 009, 010, 011, 012, 013, 014) + cross-role RBAC denials + audit trail spot-checks.
