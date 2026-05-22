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

---

# Addendum — UI Test Cases from Figma Wireframes (TimeTrack)

The following cases were added after reviewing the TimeTrack wireframes (login, dashboard, log time, my timesheets, approval queue, financial admin, admin panel). They cover UX detail not derivable from the §7 AC alone.

**Notable design observations (worth confirming with design/PO before testing):**
1. **"Final Approve + Lock" is a single combined action button** on the Financial Admin page — the spec text implies two steps (final approve, then lock). The design collapses them.
2. **Extra approval step (Herman/Deon) appears separately**, only for users requiring it, with an orange/amber "Extra Approve" CTA. Not every Lead-approved timesheet hits this stage.
3. **Team Member's sidebar shows Approvals, Reports, and Admin links** (visible to John Doe in wireframe). Per §4 RBAC these should be hidden or 403. Either the design predates RBAC, or these links resolve to "no access" pages. Test cases assume RBAC takes precedence.
4. **Team Member's "Export" action on their own Locked rows** in My Timesheets — appears to be a personal data download distinct from the billing template export. Per §4 only Financial Admin has "Export data" capability; this needs clarification.
5. **Product name and tagline** — designs use "TimeTrack — Billing Workflow System", tagline "Accurate time. Simplified billing." Spec uses generic "Timesheet & Billing Workflow System". Branding alignment needed.

---

## 8. Sign-In / Authentication

### TC-058 — Sign in with valid credentials (happy path)
**Source:** Login wireframe
**Preconditions:** A registered active user exists.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to root URL | Login page renders with TimeTrack branding, "Sign in to your account" heading, "Enter your credentials to continue" subtitle |
| 2 | Enter valid email and password | Inputs accept values; password masked as dots |
| 3 | Click Sign In | Redirected to Dashboard scoped to user's role |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-059 — Sign in with unknown email
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter `not.a.user@test.local`, any password | — |
| 2 | Click Sign In | Generic "Invalid email or password" error (must not reveal whether email exists) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-060 — Sign in with wrong password
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter valid email, wrong password | — |
| 2 | Click Sign In | Same generic "Invalid email or password" error as TC-059 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-061 — Empty email field blocks sign-in
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Leave Email blank, enter password | — |
| 2 | Click Sign In | Inline error "Email is required"; no network request fired |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-062 — Invalid email format blocks sign-in
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter "not-an-email" in email field | — |
| 2 | Tab out or click Sign In | Inline format error "Enter a valid email address" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-063 — Empty password blocks sign-in
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter valid email; leave Password blank | — |
| 2 | Click Sign In | Inline error "Password is required" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-064 — Forgot password link navigates
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Forgot password?" link | Navigates to password reset flow |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-065 — Password input is masked
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Type characters in Password field | All characters rendered as `•` / dots; not visible in plaintext |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-066 — Deactivated user cannot sign in
**Source:** Admin Panel — "Deactivate Account" control
**Preconditions:** A user with `Active=false`.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Submit valid credentials for deactivated account | Sign-in fails with "Account inactive" or generic "Invalid email or password" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 9. Dashboard (Team Member view)

### TC-067 — Dashboard greets user by first name
**Source:** Dashboard wireframe
**Preconditions:** Logged in as John Doe.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Dashboard | Heading reads "Good morning, John" (or afternoon/evening per time of day) + waving emoji |
| 2 | Subline | "X.Xh logged this week · X.Xh today" — values match DB |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-068 — KPI cards render correct values
**Source:** Dashboard — 4 KPI cards
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Load dashboard | KPI 1: "This Week / of 40h target" with hours matching sum of week entries |
| 2 | KPI 2 | "Pending / entries to review" — count of overtime-flagged or pending entries |
| 3 | KPI 3 | "Submitted / awaiting approval" — count of weeks in Submitted state |
| 4 | KPI 4 | "Approved / timesheets this month" — count of weeks Approved/Locked this calendar month |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-069 — Day cards show daily total and per-project breakdown
**Source:** Dashboard — This Week cards
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Mon 18 card | Header "Mon 18", total in bold (e.g. 8.0h), each project listed underneath with its hours |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-070 — Overtime day renders in amber/warning colour
**Source:** Dashboard — Thu 21 (9.0h shown in orange)
**Preconditions:** Day with > 8h total.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect day card with overtime hours | Total text is amber/orange; normal days remain green |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-071 — Empty day shows em-dash and "+ Add"
**Source:** Dashboard — Fri 22 "—" with "+ Add"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect day with zero entries | Card shows "—", subtitle "No entries", and a "+ Add" affordance |
| 2 | Click "+ Add" | Opens Log Time modal pre-filled with that day's date |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-072 — "+ Log Time" button opens modal
**Source:** Dashboard — primary CTA
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "+ Log Time" | Log Time modal opens; Date pre-set to today |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-073 — "Submit Week for Approval" visible only when week is Draft
**Source:** Dashboard
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Week is Draft | Submit button visible |
| 2 | After submission, return to Dashboard | Button hidden / replaced with status indicator |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-074 — Recent Activity panel lists latest 4 events newest first
**Source:** Dashboard — right column
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Recent Activity | 4 most recent events shown with relative timestamps (e.g. "2h ago", "1d ago", "5d ago") |
| 2 | Trigger a new approval | New event appears at the top after refresh; oldest drops off |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-075 — Activity colour dot matches event type
**Source:** Dashboard — Recent Activity
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect dots | Approved=green, Comment=amber, Submitted=blue, Locked=purple (or per design token set) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-076 — Sidebar nav reflects role (Team Member)
**Source:** Dashboard — left sidebar shows Approvals/Reports/Admin to John Doe (Team Member)
**Notes:** Confirms intent with design — per §4 RBAC these should be hidden or render 403 page.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Logged in as Team Member | Approvals/Reports/Admin links either hidden OR visible but clicking shows "Not authorised" content (per chosen approach) |
| 2 | Direct URL `/admin` | 403 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 10. Log Time Modal — UX specifics

### TC-077 — Required-field markers shown
**Source:** Log Time modal — Hours *, Project *, Notes *
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open modal | Hours, Project, and Notes labels show asterisk (*); Task Type and Ticket Reference are marked "(optional)" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-078 — Overtime warning banner appears and is non-blocking
**Source:** Log Time modal — amber banner under Ticket Reference
**Preconditions:** User attempts to log hours that push the day total over 8h.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter Hours=9 | Banner appears: "⚠ 9h exceeds the 8h daily threshold — flagged for Team Lead review" |
| 2 | Secondary line text | "This does not block submission." |
| 3 | Click Save Entry | Save succeeds; entry flagged for review |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-079 — Save Entry disabled until required fields valid
**Source:** Log Time modal — Save Entry CTA
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open modal — only Date pre-set | Save Entry button disabled (muted state) |
| 2 | Fill Hours, Project, Notes | Save Entry becomes enabled |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-080 — Cancel discards entry without saving
**Source:** Log Time modal — Cancel button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Fill all fields | — |
| 2 | Click Cancel | Modal closes; no entry persisted; day card unchanged |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-081 — Project dropdown lists only assigned projects
**Source:** Log Time modal — Project dropdown showing "Project Alpha — Mobile App"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Project dropdown | Lists only projects the logged-in user is assigned to |
| 2 | Compare against admin assignment | Match exactly; no out-of-scope projects |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-082 — Ticket Reference accepts placeholder format
**Source:** Log Time modal — placeholder "PROJ-142 (DevOps / JIRA / Linear)"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Ticket Reference field empty state | Placeholder text matches the format hint |
| 2 | Enter "JIRA-456"; Save | Persists as-is — manual entry allowed per §9 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-083 — Modal closes on ESC
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Log Time modal | Modal visible |
| 2 | Press Esc | Modal closes; no entry saved (same as Cancel) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 11. My Timesheets — Tabs and Actions

### TC-084 — Tab filters: All / Draft / Submitted / Approved / Locked
**Source:** My Timesheets wireframe
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open My Timesheets, All tab selected by default | All weeks shown |
| 2 | Click Draft | Only Draft weeks shown |
| 3 | Click Submitted | Only Submitted shown |
| 4 | Click Approved | Both "Approved by Lead" and "Final Approved" shown |
| 5 | Click Locked | Only Locked shown |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-085 — Status badge colour-coded per state
**Source:** My Timesheets — pills
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect each row's status pill | Submitted=blue, Approved by Lead=green, Locked=purple — match design tokens |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-086 — Approver column populated post-approval
**Source:** My Timesheets — Approver column
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Row with status Submitted | Approver shows latest approver-in-queue (Lead) or blank — confirm with PO |
| 2 | Row with Locked | Approver shows the user who locked (e.g. "Herman F.") |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-087 — View action available on non-Locked rows
**Source:** My Timesheets — Actions column
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click View on a Submitted row | Detail page opens read-only |
| 2 | Click View on Approved by Lead | Detail page opens read-only |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-088 — Export action visible on Locked rows (personal download)
**Source:** My Timesheets — Export action on Locked rows for own weeks
**Notes:** Conflict with §4 — please confirm scope. Assumed: Team Member can export their OWN locked data only.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Export on own Locked row | File downloads containing only the user's own locked entries for that week |
| 2 | Compare to billing template export | Same row schema but scoped to single user |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 12. Approval Queue (Team Lead) — UX specifics

### TC-089 — Queue header reflects pending count
**Source:** Approval Queue — "4 timesheets pending your review"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Approvals page | Subtitle count matches number of rows below |
| 2 | Approve one entry | Count decrements by 1 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-090 — Overtime flag badge shown when applicable
**Source:** Approval Queue — Sarah Chen row "⚠ 2 overtime entries flagged"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect row with overtime entries | Amber badge shown beneath the name with accurate count |
| 2 | Inspect row without overtime | No badge |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-091 — Reject opens comment panel; Send Rejection requires comment
**Source:** Approval Queue — Reject flow
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Reject on a row | "Reject — {Name}" panel appears with required Comments textarea |
| 2 | Click Send Rejection with empty textarea | Button disabled OR error "Comments are required" |
| 3 | Enter comment and click Send Rejection | Rejection submitted; row removed from queue |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-092 — Send Rejection styled as destructive
**Source:** Approval Queue — red Send Rejection button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Send Rejection button | Red/destructive colour token applied |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-093 — Cancel on reject panel closes without action
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Reject panel, type comment | — |
| 2 | Click Cancel | Panel closes; row remains in queue; no rejection sent |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-094 — View details opens timesheet read-only
**Source:** Approval Queue — View details button per row
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click View details on a row | Timesheet detail opens with daily breakdown, totals, comments, and audit |
| 2 | Approve / Reject controls available on detail page | Same affordances as queue row |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 13. Financial Admin Page

### TC-095 — Page sections present
**Source:** Financial Admin wireframe — left "Awaiting Final Approval", right "Reports & Export"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Financial Admin page | Header shows "Final approval · Locking · Reporting · Export" |
| 2 | Inspect layout | Left column: Awaiting Final Approval queue + Extra Approval section. Right column: Reports & Export panel + Audit Trail |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-096 — "Final Approve + Lock" performs a single combined action
**Source:** Financial Admin — green CTA per row
**Notes:** Design conflicts with sequential spec phrasing — confirm with PO.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Final Approve + Lock" on a Lead-approved row | Confirmation shown |
| 2 | Confirm | Status transitions directly to Locked; row removed from queue; audit shows BOTH "Final Approved" and "Locked" events with same actor and timestamp |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-097 — Extra Approval section only shown for flagged users
**Source:** Financial Admin — "Herman / Deon — Extra Approval Step" with amber CTA
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Extra Approval section | Only users requiring extra sign-off listed |
| 2 | If no users require extra approval | Section hidden or shows "No items require extra approval" |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-098 — Extra Approve button styled amber/warning
**Source:** Financial Admin — orange "Extra Approve — Sarah Chen (18–22 May)"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect button colour | Amber / warning token distinct from primary blue and Final Approve + Lock green |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-099 — Reports panel: Daily / Weekly / Monthly Generate buttons
**Source:** Financial Admin — Reports & Export, "Locked entries only" subtitle
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Generate on Daily Reconciliation | Daily report for today produced, scoped to Locked entries only |
| 2 | Click Generate on Weekly | Current week report |
| 3 | Click Generate on Monthly | Current month report |
| 4 | Verify Draft/Submitted/Approved-but-not-locked entries excluded | None of those entries appear |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-100 — Export Billing Template (.xlsx)
**Source:** Financial Admin — green primary export button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Export Billing Template (.xlsx)" | File downloads as .xlsx |
| 2 | Open file | Schema matches billing template; contains only Locked entries |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-101 — Export Audit Trail (.csv)
**Source:** Financial Admin — secondary Export Audit Trail button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Export Audit Trail (.csv)" | File downloads as .csv |
| 2 | Inspect contents | Contains audit rows: Actor, Action (Created/Edited/Approved/Locked), Target, Timestamp, Before/After |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-102 — Audit Trail panel shows newest events first
**Source:** Financial Admin — right-column Audit Trail list
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Audit Trail | Each entry: Actor (e.g. "Herman F."), Action ("Locked — Sarah Chen 18–22 May"), relative timestamp |
| 2 | Trigger a new lock | Entry appears at top after refresh |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-103 — Non-FA roles cannot access Financial Admin page
**Source:** §4 + page sensitivity
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Sign in as Team Member; navigate to Financial Admin URL | 403 |
| 2 | Sign in as Team Lead | 403 |
| 3 | Sign in as Admin (scoped) | 403 (financial visibility hidden per §4 banner) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 14. Admin Panel (Scoped)

### TC-104 — Scoped access banner displayed
**Source:** Admin Panel wireframe — locked-icon banner
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Logged in as Admin (Alex), open Admin Panel | Banner text: "🔒 Scoped access — you can only view and manage users and projects assigned to your scope. Financial data is not visible." |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-105 — Tabs: Users / Projects / Teams
**Source:** Admin Panel — tab strip
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Default tab | Users |
| 2 | Click Projects | Projects-tab content loads (billing-type + assigned-users mgmt per design footer note) |
| 3 | Click Teams | Teams content loads |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-106 — Search filters users by name
**Source:** Admin Panel — search input
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter "Sarah" in search | Only users matching "Sarah" shown |
| 2 | Clear search | Full scoped list returns |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-107 — Role and Team filter dropdowns
**Source:** Admin Panel — Role: All, Team: All dropdowns
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Filter Role = Team Lead | Only Team Leads visible |
| 2 | Filter Team = Alpha Squad | Only Alpha Squad members visible |
| 3 | Combine filters | Intersection (Alpha Squad Team Leads) shown |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-108 — Click row opens Edit User drawer
**Source:** Admin Panel — Edit User right-side drawer
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click on a user row | Right-side drawer slides in showing User Details (avatar, name, email), Role, Team Assignment, Assigned Projects, Account Status |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-109 — Assigned Projects chip removal
**Source:** Edit User — chips with × icon
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click × on "Project Beta" chip | Chip removed visually; assignment pending save |
| 2 | Click Save Changes | Assignment removed in DB; user no longer sees Project Beta in their dropdown |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-110 — Add available project to user
**Source:** Edit User — "Available to assign" + "+ Add"
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click + Add on Project Gamma | Project Gamma appears in Assigned Projects chips |
| 2 | Save Changes | User sees Project Gamma in their dropdown next session |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-111 — "Available to assign" only lists projects in Admin's scope
**Source:** §4/§7 scoped access + Edit User panel
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Admin (Alex) opens Edit User for any in-scope user | Available list contains only projects Alex is scoped to (no out-of-scope projects leaked) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-112 — Role dropdown change persisted
**Source:** Edit User — Role dropdown
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Change Sarah Chen from Team Member to Team Lead | Dropdown updates |
| 2 | Save Changes | User's role updated; next sign-in shows Team Lead sidebar |
| 3 | Verify audit | Role-change event logged with Actor=Alex, Before/After |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-113 — Admin cannot elevate user to Financial Admin (scope guard)
**Source:** §4 RBAC + scoped Admin
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect Role dropdown options | Financial Admin option NOT present (only roles within Admin's authority) |
| 2 | Attempt via API to set role=Financial Admin | 403 / validation error |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-114 — Deactivate Account confirmation + effect
**Source:** Edit User — red Deactivate Account button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click Deactivate Account | Confirmation dialog: "Deactivate {Name}? They will no longer be able to sign in." |
| 2 | Confirm | Account Status pill changes to Inactive |
| 3 | Attempt sign-in as that user | Fails (per TC-066) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-115 — Cancel discards drawer changes
**Source:** Edit User — Cancel button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Edit User, make changes (role, projects) | Changes shown locally |
| 2 | Click Cancel | Drawer closes; no changes persisted on reopen |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-116 — Save Changes persists and confirms
**Source:** Edit User — primary Save Changes button
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Make a valid change; click Save Changes | Success toast / confirmation; drawer closes |
| 2 | Reopen same user | Persisted values match what was saved |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

## 15. Mobile / Responsive Behaviour

**Scope:** Mobile/tablet behaviour of all flows already covered in §1–§14. These cases test layout reflow, touch interaction, mobile browser quirks, and accessibility on touch devices — they do NOT re-verify business logic (that's done by the desktop cases).

**Wireframe basis:** Cases validated against the Figma mobile wireframes M1–M6 (390×844) for Login, Dashboard, Log Time Entry, My Timesheets, Approvals (Team Lead), Admin Panel. Two mobile-specific design choices noted upfront:
- **Navigation = 5-tab bottom bar** (Home / Log / Sheets / Approvals / More — replaced with "Admin" for admin-scope users), NOT a hamburger drawer.
- **Approvals = swipe gestures** (right approves, left rejects), NOT bulk-select checkboxes.

**Wireframes NOT supplied (cases inferred):** Financial Admin mobile view (TC-131), Reject-reason capture sheet (referenced in TC-130), Week detail drill-in (TC-145), Add-User form (TC-152). Flag these as PO follow-ups.

**Possible design conflict #6:** Mobile Log Time form includes a "Task Type" dropdown that does not appear in the desktop Log Time modal. Raise with UX before sign-off — should be added alongside the 5 design conflicts already flagged at the top of this file.

**Shared mobile preconditions (assumed for all cases in this section):**
- Devices under test: iPhone 14 (390×844) iOS Safari 17+, Pixel 7 (412×915) Android Chrome current, iPad portrait (768×1024) iOS Safari, small Android (360×640) Chrome.
- Breakpoints assumed: ≤767px = mobile, 768–1023px = tablet, ≥1024px = desktop.
- Touch input only (no mouse hover events).
- All seeded users from the shared preconditions block at the top of this file are available.

---

### TC-117 — 5-tab bottom navigation bar on mobile
**AC:** UX (Figma M2 / M4 / M5 / M6 — bottom-tab pattern)
**Preconditions:** Logged in as `tm1@test.local` on iPhone 14 Safari.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Load Dashboard | Bottom tab bar visible with 5 tabs: Home / Log / Sheets / Approvals / More. "Home" tab styled active (blue label/indicator). |
| 2 | Tap "Sheets" tab | Navigates to My Timesheets; "Sheets" becomes active; "Home" indicator clears |
| 3 | Tap "Log" tab | Opens Log Time Entry (full-screen) |
| 4 | Sign out, sign in as `admin1@test.local`, reload | Fifth tab label = "Admin" (replaces "More" for admin-scope users) |
| 5 | Sign in as `tm1@test.local` (Team Member) | Fifth tab = "More" again |
| 6 | Verify "Approvals" tab on Team Member account | Per RBAC design conflict #2 (TC-076), tab should be hidden OR tap → 403. Wireframe shows it visible on M2 (Team Member dashboard) — confirm desired behaviour with UX. |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-118 — Per-screen mobile header layout
**AC:** UX (Figma M1–M6)
**Preconditions:** Set of seeded users across roles; iPhone 14.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Signed out, visit `/signin` (M1) | Dark hero with TT mark + "TimeTrack" + "Billing Workflow System" branding fills upper half; no top nav |
| 2 | Signed in, Dashboard (M2) | Header: "Hi, {firstName} 👋" / "{day}, {date}" subline / initials avatar top-right |
| 3 | Log Time Entry (M3) | Header: "Log Time Entry" title + "Save" link top-right |
| 4 | My Timesheets (M4) | Header: "My Timesheets" + "+ New" button top-right |
| 5 | Approvals (M5) | Header: "Approvals" + red circular pending-count badge |
| 6 | Admin Panel (M6) | Header: "Admin Panel" + "+ Add" button top-right |
| 7 | Confirm no hamburger anywhere | No hamburger icon on any screen; bottom-tab bar is the only persistent navigation |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-119 — Touch targets meet WCAG 2.5.5 minimum (44×44 px)
**AC:** Accessibility — WCAG 2.1 AA target size
**Preconditions:** Any mobile device.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Inspect or measure primary CTAs: Save, Submit Week, Approve, Reject, Final Approve + Lock, Export, Sign In | Each ≥ 44×44 CSS px including padding |
| 2 | Inspect row-level icon actions (Edit, Delete, Resubmit) | Each ≥ 44×44 px; spacing ≥ 8px between adjacent targets |
| 3 | Inspect hamburger, user avatar, drawer items | Each ≥ 44×44 px |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-120 — Sign-in form usable with mobile keyboard open
**AC:** UX — auth on mobile
**Preconditions:** Signed out; iPhone 14, Safari.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Load `/signin` | Email + Password + Sign In button all visible above the fold |
| 2 | Tap Email field | Keyboard opens; Sign In button remains reachable (scrollable to or sticky), not hidden behind keyboard |
| 3 | Fill credentials, tap Sign In | Sign in succeeds; redirect to dashboard |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-121 — Password manager autofill works on iOS / Android
**AC:** UX — auth on mobile
**Preconditions:** Saved credentials for `tm1@test.local` in iOS Keychain (or Android autofill).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open `/signin`, tap Email field | Autofill suggestion bar appears above keyboard |
| 2 | Tap suggestion | Email AND password fields both fill |
| 3 | Tap Sign In | Auth succeeds |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Verifies `autocomplete="email"` and `autocomplete="current-password"` attributes are present.

---

### TC-122 — Log Time Entry opens as full-screen page
**AC:** UX (Figma M3)
**Preconditions:** Logged in as `tm1@test.local` on iPhone 14.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | From Dashboard, tap any of: "+ Log Time" Quick Action, "+" FAB, "Log" bottom tab | Log Time Entry opens taking the full viewport — NOT a centered desktop modal with whitespace |
| 2 | Inspect header | "Log Time Entry" title + "Save" link top-right; no X icon |
| 3 | Inspect form layout | Date (left) + Hours with "h" suffix (right) on one row; Project full-width; Task Type full-width (optional, NEW vs desktop); Notes textarea (required); Ticket Reference (optional) |
| 4 | Scroll to bottom of form | "Save Entry" primary button (full-width blue) above "Cancel" button (text/outlined) |
| 5 | Tap Cancel | Returns to previous screen; no entry created |
| 6 | Reopen, verify no horizontal scroll on any field at 390px viewport | All fields fit; placeholders not truncated |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Two save affordances (header "Save" link + bottom "Save Entry" button) — TC-142 verifies parity.

---

### TC-123 — Hours field opens numeric keypad
**AC:** UX — form input semantics
**Preconditions:** Log Time modal open on iPhone 14.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap Hours field | Numeric keypad appears (with decimal point); NOT full QWERTY |
| 2 | Tap Notes field | Full text keyboard appears |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Verifies `inputmode="decimal"` (or `type="number"` with `step="0.25"`) on Hours.

---

### TC-124 — Date picker is native on iOS / Android
**AC:** UX — touch input
**Preconditions:** Log Time modal open.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap Date field on iOS Safari | Native iOS date wheel/calendar appears |
| 2 | Pick a date, tap Done | Field populates; picker dismisses; no custom JS picker overlay covers the form |
| 3 | Repeat on Android Chrome | Material-style native picker appears and behaves equivalently |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-125 — iOS Safari does not auto-zoom on input focus
**AC:** UX — iOS Safari quirk
**Preconditions:** Any form (sign-in, Log Time) on iPhone Safari.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap into Email / Notes / Hours field | Viewport does NOT zoom in; page font/layout unchanged |
| 2 | Inspect input CSS | `font-size` ≥ 16px on all interactive text inputs |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-126 — My Timesheets renders as weekly-summary list on mobile
**AC:** UX (Figma M4)
**Preconditions:** Logged in as `tm1@test.local` with ≥5 historical weeks in mixed states (Submitted, Approved by Lead, Locked, Draft).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to My Timesheets on iPhone 14 | Vertical list of weekly rows. Each row: "{date range, e.g. 18–22 May 2026}" left + "{total hours, e.g. 32.5h}" + status badge + chevron `›`. NO desktop 7-column grid; NO horizontal scroll. |
| 2 | Verify status badges | Submitted, Approved by Lead, Locked, Draft — all visible without truncation; contrast meets AA |
| 3 | Verify ordering | Most recent week at top; descending by start date |
| 4 | Verify "+ New" CTA top-right | Always visible (TC-146 covers behaviour) |
| 5 | Tap a row | Drills into that week's detail view (TC-145) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-127 — Status badges and row actions readable on small screen
**AC:** UX — mobile readability
**Preconditions:** My Timesheets on 360×640 Android.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View list with Draft / Submitted / Approved / Locked / Flagged entries | All badge text legible (no truncation, contrast meets AA) |
| 2 | Locate row-level actions (Edit, Delete, Resubmit) | Visible inline OR accessible via overflow `⋯` menu — tap overflow → menu opens above keyboard area |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-128 — Export triggers file download on mobile
**AC:** ST-014 — Export on mobile
**Preconditions:** Logged in as `tm1@test.local`; at least one Locked timesheet.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | On iPhone Safari, tap Export on a Locked row | iOS download/share sheet appears; file is offered as CSV or PDF |
| 2 | Save to Files | File saved; opening shows correct content |
| 3 | Repeat on Android Chrome | File downloads to Downloads/; notification appears |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Re-confirm scope of Team Member Export per design conflict #3 (TC-088) before testing.

---

### TC-129 — Swipe right approves a timesheet
**AC:** ST-007 / ST-008 (Figma M5)
**Preconditions:** Logged in as `lead1@test.local`; ≥1 Submitted timesheet pending (e.g. Mark Peters · 38h · Week of 18–22 May).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Approvals on iPhone 14 | Each pending row shows avatar / name / role · hours / week range / chevron. Tip text below list: "Swipe left to reject · right to approve" |
| 2 | Touch-drag a row left-to-right past threshold (~50% width) | Green "Approve" background revealed; on release row animates out |
| 3 | Verify toast | "Timesheet approved" (or equivalent) appears |
| 4 | Verify header pending count badge decremented | "4" → "3" (or current −1) |
| 5 | Verify DB / desktop view | Status = Approved by Lead; ApprovedBy = lead1; ApprovedAt set |
| 6 | Repeat swipe right but release before threshold (~20%) | Row springs back; no action taken |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-130 — Swipe left rejects a timesheet
**AC:** ST-008 (Figma M5)
**Preconditions:** Logged in as `lead1@test.local`; ≥1 Submitted timesheet.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Swipe a row right-to-left past threshold | Red "Reject" background revealed; on release, reject flow triggers |
| 2 | Reject-reason capture | A modal/sheet prompts for reason. Enter reason, confirm. |
| 3 | Verify row removal + toast | Row animates out; "Timesheet rejected" toast; pending badge decrements |
| 4 | Verify DB / desktop | Status = Rejected; RejectionReason persisted |
| 5 | Repeat swipe left, cancel reason dialog mid-flow | Row returns to queue; no state change |
| 6 | Verify bulk-select is NOT present | No checkboxes on rows, no "select all" affordance — mobile design intentionally omits bulk actions (PO follow-up if needed) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Reject-reason capture screen not in supplied wireframes — PO follow-up. Without it, swipe-left cannot ship.

---

### TC-131 — Final Approve + Lock reachable on Financial Admin mobile view
**AC:** ST-010 / ST-011 on mobile
**Preconditions:** Logged in as `fa@test.local`; at least one Team-Lead-Approved timesheet awaiting final approval.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Financial Admin page on iPhone 14 | Pending list visible (layout TBC — mobile wireframe not supplied) |
| 2 | Tap a row to open detail | Detail view fits viewport; Final Approve + Lock CTA visible without scrolling past key data |
| 3 | Tap Final Approve + Lock | Confirm modal fits viewport; tap Confirm → status moves to Locked |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Financial Admin mobile wireframe NOT in supplied set — flagged as PO follow-up. Also depends on design conflict #1 (TC-096) resolution.

---

### TC-132 — Admin Panel reflows with Users / Projects / Teams tab strip
**AC:** ST-012 / ST-013 (Figma M6)
**Preconditions:** Logged in as `admin1@test.local` (scoped to P1 / Alpha Squad).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "Admin" bottom tab on iPhone 14 | Admin Panel opens; header "Admin Panel" + "+ Add"; tab strip "Users | Projects | Teams" with Users active by default |
| 2 | Tap "Projects" tab | Tab content switches; only in-scope projects listed (P1; NOT P2) |
| 3 | Tap "Teams" tab | Only in-scope teams listed (Alpha Squad; NOT Beta Squad) |
| 4 | Return to "Users" tab | Users content restored; previous scroll position retained |
| 5 | Confirm scope banner persists across tabs | "ⓘ You see only your assigned scope." banner visible on every tab |
| 6 | Confirm NO "desktop only" fallback notice | Admin Panel is a primary mobile surface, not a desktop-only feature |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-133 — Orientation change preserves form state
**AC:** UX — mobile robustness
**Preconditions:** Log Time modal open; iPhone 14.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Fill Project=P1, Hours=4, Notes="rotate test" | Values populated |
| 2 | Rotate device portrait → landscape | Layout reflows for landscape; entered values preserved |
| 3 | Rotate back to portrait | Values still preserved; layout returns |
| 4 | Tap Save | Entry saves correctly |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-134 — Toasts position above sticky elements / keyboard
**AC:** UX — feedback visibility
**Preconditions:** Log Time modal open on iPhone 14.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Save a draft entry while keyboard is up | Success toast appears within visible viewport (not behind keyboard, not behind sticky footer) |
| 2 | Toast auto-dismisses or is dismissible | After 4–5s, toast fades; or X dismisses it |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-135 — Sticky primary CTA on long forms
**AC:** UX — mobile forms
**Preconditions:** Log Time modal open on iPhone 14 (full-screen sheet).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Notes field, type ≥3 lines | Save button remains anchored at bottom of viewport (above keyboard) OR is reachable by scrolling within the sheet without leaving the form |
| 2 | Hours field validation error appears | Error message visible; Save remains usable |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-136 — Slow 3G shows skeleton, not blank screen
**AC:** UX — perceived performance on mobile networks
**Preconditions:** Chrome DevTools network throttle = "Slow 3G", or real low-bandwidth device.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Hard reload `/dashboard` | Page chrome renders; skeleton placeholders shown for KPI tiles and timesheet preview |
| 2 | Wait | Data populates within 10s; no blank white screen; no spinner-only state >2s |
| 3 | Trigger Log Time save under throttle | Optimistic UI OR spinner on Save button with disabled state — no double-submit possible |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-137 — VoiceOver / TalkBack reads form labels and button states
**AC:** Accessibility — screen reader on mobile
**Preconditions:** iPhone 14 with VoiceOver enabled (Settings → Accessibility → VoiceOver).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Swipe through Log Time modal | Each field announces: label ("Project"), role ("combobox"/"text field"), current value if set |
| 2 | Required field announces "required" | Yes |
| 3 | Save button announces label + state ("Save, button, disabled" when form invalid) | Yes |
| 4 | Repeat with Android TalkBack | Equivalent behaviour |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-138 — Pinch-zoom does not break layout
**AC:** Accessibility — WCAG 1.4.4 Resize Text
**Preconditions:** Any page on iPhone Safari.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Pinch-zoom in to ~200% | Content zooms; user can scroll to see content; NO `user-scalable=no` blocking zoom |
| 2 | Pinch back out | Layout returns to default; no horizontal scroll introduced; no overlapping elements |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-139 — Dashboard FAB opens Log Time Entry
**AC:** UX (Figma M2)
**Preconditions:** Logged in as `tm1@test.local` on iPhone 14.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Load Dashboard | Floating Action Button (blue circle, white "+") visible bottom-right, above the bottom tab bar |
| 2 | Tap FAB | Log Time Entry opens (full-screen) |
| 3 | Cancel out of Log Time | Returns to Dashboard; FAB visible again |
| 4 | Scroll "Today's Entries" list down/up | FAB remains anchored (sticky) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-140 — Dashboard Quick Actions row navigates correctly
**AC:** UX (Figma M2)
**Preconditions:** Logged in as `tm1@test.local`.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | On Dashboard, locate "Quick Actions" row of 3 buttons | "+ Log Time" (filled blue primary), "My Timesheets" (outlined), "Approvals" (outlined) — all visible in a single row |
| 2 | Tap "+ Log Time" | Log Time Entry opens |
| 3 | Cancel back, tap "My Timesheets" | Navigates to My Timesheets; "Sheets" bottom-nav tab becomes active |
| 4 | Back, tap "Approvals" | Team Lead: navigates to Approvals queue. Team Member: hidden/disabled/403 per design conflict #2 (TC-076) resolution. |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-141 — Dashboard "This Week" progress card accuracy
**AC:** UX (Figma M2); ST-002
**Preconditions:** Logged in as `tm1@test.local` with 32.5h logged this week against 40h expected; 7.5h logged today.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Load Dashboard | "This Week" card shows "32.5h / 40h" headline; progress bar ~81% filled; subline "81% complete · 7.5h today" |
| 2 | Log 1.5h additional today | Headline updates to "34.0h / 40h"; today total updates to "9.0h today" |
| 3 | Continue logging until 40h reached | Bar 100%; "100% complete" or "Complete" copy |
| 4 | Log past 40h (overtime) | Bar capped at 100% OR overflow indicator; overtime flag surfaced for Team Lead approval (see TC-148) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-142 — Log Time dual save affordances behave identically
**AC:** ST-001; UX (Figma M3)
**Preconditions:** Logged in as `tm1@test.local`; Log Time Entry open.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Fill valid entry (Project=P1, Hours=8.0, Notes="x") | Form valid |
| 2 | Tap header "Save" link | Entry persists; returns to Dashboard; Today's Entries reflects new row |
| 3 | Reopen Log Time, fill another valid entry | — |
| 4 | Tap bottom "Save Entry" button | Identical behaviour — entry persists, returns to Dashboard |
| 5 | With Notes blank (invalid), tap header "Save" | Inline validation error on Notes; no save |
| 6 | With Notes blank, tap bottom "Save Entry" | Same validation error; no save |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-143 — Hours-exceeds-threshold inline warning banner
**AC:** ST-001 (c) on mobile (Figma M3)
**Preconditions:** Logged in as `tm1@test.local`; daily expected threshold = 8h.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Log Time, set Hours = 9 | Inline warning banner appears between form fields and Save: "⚠ 9h exceeds threshold — flagged for review / Will not block submission" |
| 2 | Set Hours = 8 | Banner disappears |
| 3 | Set Hours = 12 | Banner re-appears, value reflects "12h" |
| 4 | Tap Save Entry with Hours = 9 | Entry saves with overtime flag = true; returns to Dashboard |
| 5 | View entry on Dashboard / My Timesheets | Visual overtime indicator present (red dot, badge, etc.) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Mobile wireframe shows Hours field "8.0 h" but warning copy says "9h exceeds threshold" — minor wireframe data inconsistency, flag to UX.

---

### TC-144 — Log Time Cancel discards form state
**AC:** UX (Figma M3)
**Preconditions:** Log Time Entry open with partially filled form.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Fill Project=P1, Hours=4, Notes="abc" | Form has values |
| 2 | Tap Cancel | EITHER returns to previous screen immediately OR prompts "Discard changes?" |
| 3 | If prompt: tap Discard | Returns to previous screen; no entry saved |
| 4 | If prompt: tap "Keep editing" | Stays on Log Time with values preserved |
| 5 | Reopen Log Time | Form starts blank — values NOT persisted as draft |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Confirm with UX whether Cancel needs a confirm prompt when form is dirty.

---

### TC-145 — My Timesheets row drill-in opens week detail
**AC:** UX (Figma M4)
**Preconditions:** Logged in as `tm1@test.local`; week 18–22 May in Submitted state with multiple per-day entries.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "18–22 May 2026" row in My Timesheets list | Drills into week detail view |
| 2 | Verify detail content | Per-day entries listed (Mon–Fri); per-day and weekly totals match the summary row; week-level status badge (Submitted) shown |
| 3 | Tap browser/native back | Returns to weekly list; scroll position retained |
| 4 | Tap a Locked-week row | Detail opens read-only — Edit / Delete actions disabled per ST-011 |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Week detail screen wireframe not in supplied set — PO follow-up for layout sign-off.

---

### TC-146 — "+ New" on My Timesheets opens Log Time and returns to list
**AC:** UX (Figma M4); ST-001
**Preconditions:** Logged in as `tm1@test.local`; on My Timesheets.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "+ New" top-right | Log Time Entry opens (full-screen) |
| 2 | Fill a valid entry and tap Save Entry | Returns to My Timesheets (NOT Dashboard, since origin was Sheets) |
| 3 | Verify the week containing the new entry shows updated total | Hours sum and status reflect the new entry |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-147 — Approvals header pending-count badge
**AC:** UX (Figma M5)
**Preconditions:** Logged in as `lead1@test.local`; exactly 4 Submitted timesheets in queue.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Approvals tab | Header: "Approvals" + red circular badge "4" |
| 2 | Swipe-right approve one row | Badge updates to "3" |
| 3 | Swipe-left reject one row (complete reason flow) | Badge updates to "2" |
| 4 | Drain queue to 0 | Badge disappears OR shows empty-state copy ("No pending approvals") |
| 5 | New submission arrives (tm1 submits a week on desktop) | Badge re-appears with "1" |
| 6 | Verify bottom-tab "Approvals" also shows count indicator if designed | Per wireframe — tab has no badge but header does; confirm whether dual indicator is required |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-148 — Overtime indicator on Approval rows with overtime entries
**AC:** UX (Figma M5); §9 overtime flag
**Preconditions:** Logged in as `lead1@test.local`; Sarah Chen (or equivalent) has 42h with at least one overtime-flagged entry for week 18–22 May.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Approvals; locate Sarah Chen row | Row shows red dot/indicator below name; "Overtime entries" label visible in subline |
| 2 | Verify rows for users without overtime (Mark Peters, Amy Johnson) | No red dot; no "Overtime entries" label |
| 3 | Tap Sarah Chen row OR swipe to inspect | If detail opens, overtime entries highlighted within |
| 4 | Approve via swipe right | Approval succeeds; overtime entries pass downstream to billing/XP as normal (verify on desktop / DB) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-149 — Admin Panel scope banner shown for scoped admins
**AC:** ST-012 (Figma M6)
**Preconditions:** Logged in as `admin1@test.local` (scoped). If a full-access admin is seeded, also test that account for negative.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Admin Panel as `admin1` | Blue info banner: "ⓘ You see only your assigned scope." visible directly under tab strip |
| 2 | Switch tabs (Users / Projects / Teams) | Banner persists across all three tabs |
| 3 | Open as Full Admin (if seeded) | Banner absent OR shows full-access copy |
| 4 | Verify Projects tab content for `admin1` | P1 visible; P2 absent |
| 5 | Verify Teams tab content | Alpha Squad visible; Beta Squad absent |
| 6 | Verify Users tab content | Only Alpha Squad members visible |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-150 — Admin Panel search filters Users list
**AC:** UX (Figma M6)
**Preconditions:** Logged in as `admin1@test.local`; Users tab active with ≥5 users in scope.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "Search users..." input | Mobile text keyboard opens |
| 2 | Type "sarah" | List filters to users whose name matches "sarah" (case-insensitive); non-matches hide |
| 3 | Clear search | Full in-scope list restores |
| 4 | Type "zzzzz" (no match) | Empty state shown ("No users match" or equivalent) |
| 5 | Type a name belonging to an out-of-scope user (e.g. Beta Squad member) | NOT returned — search respects admin scope |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-151 — Admin Panel user status dot color reflects active/inactive
**AC:** UX (Figma M6)
**Preconditions:** Logged in as `admin1@test.local`; ≥1 active and ≥1 deactivated user in scope (e.g. James Nkosi inactive).
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | On Users tab, inspect active user row | Green status dot to right of row, before chevron |
| 2 | Inspect deactivated user row (James Nkosi) | Gray status dot |
| 3 | Tap an active user, deactivate via drawer, save (uses TC-114 flow) | Returns to list; dot updates to gray without full reload |
| 4 | Reactivate same user | Dot returns to green |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested

---

### TC-152 — Admin Panel "+ Add" opens Add User form within scope
**AC:** ST-012 (Figma M6)
**Preconditions:** Logged in as `admin1@test.local`.
**Test Steps**
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "+ Add" top-right | Add User form opens (full-screen on mobile, consistent with Log Time pattern) |
| 2 | Tap Cancel | Returns to Admin Panel; no user created |
| 3 | Reopen, fill required fields, save | New user created within admin's scope (Alpha Squad / P1); appears in Users list; status dot green by default |
| 4 | Try to assign Team = Beta Squad (out of scope) | Option not selectable OR validation error "Out of scope" |
| 5 | Try to assign Role = Financial Admin | Blocked per design conflict — Admin cannot elevate to FA (TC-113) |

**Status:** [ ] Pass [ ] Fail [ ] Blocked [ ] Not Tested
**Notes:** Add User form wireframe not in supplied set — assume same full-screen pattern as Log Time. PO follow-up for layout sign-off.

---

## 16. Out of scope for this suite
- Integrations (ST-015) — DevOps/JIRA/Linear ticket validation: requires live integration; covered as a separate harness once endpoints exist.
- Future ST-018 resource planning: explicitly out of scope per §2.
- Password reset flow (TC-064 covers nav only): full flow tested separately once reset screens are available.
- Native mobile apps: this suite covers responsive web only. Native iOS/Android apps (if planned) need their own suite.
- Real-device farm coverage beyond iPhone 14 / Pixel 7 / iPad portrait: BrowserStack matrix expansion deferred until baseline mobile cases pass.

---

**Total cases:** 152 (57 AC-based + 59 UI/wireframe-derived desktop + 36 mobile/responsive)
**Coverage:**
- §7 Acceptance Criteria (ST-001, 002, 003, 007, 008, 009, 010, 011, 012, 013, 014)
- Cross-role RBAC denials
- Audit trail spot-checks
- TimeTrack UI flows from Figma desktop wireframes (login, dashboard, log time modal, my timesheets, approval queue, financial admin, admin panel)
- TimeTrack mobile wireframes M1–M6 (bottom-tab nav, FAB, full-screen log time, weekly-summary list, swipe approve/reject, scope-banner admin panel, orientation, network, a11y on touch)
- 6 spec/design conflicts flagged for PO confirmation (5 desktop + Task Type discrepancy on mobile)
- PO follow-ups: reject-reason mobile sheet, FA mobile view, week-detail mobile view, Add-User mobile form
