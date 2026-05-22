---
name: smoke-test
description: Post-deployment / post-build smoke test on TimeQuest. Walks through a fixed checklist (Identity login for all 3 seeded roles, navigation across /, /timesheet, /approvals, /leaderboard, /badges, role-based menu, TimeEntry submission, Manager approval triggering XP + Level + BadgeAward) and outputs a PASS / FAIL verdict to gate further feature testing. Read-only.
---

Help the user run a post-deployment / post-build smoke test on TimeQuest. Ask for the details below if not provided, then output the completed checklist.

RULES:
- Default environment is Local Dev unless the user specifies otherwise.
- Always ask which target was deployed/built, the deployment / build time, who did it, and the linked PR / release.
- Tester is LydiaB.
- This is a READ ONLY verification — do not suggest or make any changes to data or code.

---

**Smoke Test — Post Deployment Verification**

**Target:** [Local Dev / Dev / Staging / Prod]
**Deployed / built:** [Date and time]
**Deployed / built by:** [Developer name]
**Tested by:** LydiaB
**Linked PR / Release:** [PR number or release link]

---

**Authentication (ASP.NET Core Identity)**
- [ ] Can log in with the seeded Admin credentials
- [ ] Can log in with the seeded Manager credentials
- [ ] Can log in with the seeded Employee credentials
- [ ] Invalid credentials show correct error
- [ ] Session persists after page refresh
- [ ] Log out clears the session

**Navigation**
- [ ] Main nav loads without errors
- [ ] All primary routes resolve (/, /timesheet, /approvals, /leaderboard, /badges)
- [ ] No console errors on page load
- [ ] Role-based menu items respect the logged-in role (Admin sees Admin areas; Employee does not see /approvals)

**Timesheet (Employee)**
- [ ] Can submit a new TimeEntry
- [ ] Submitted entry shows on the user's timesheet list with Status = Submitted
- [ ] Validation prevents zero / negative hours
- [ ] Validation prevents future-dated entries (if AC requires)

**Approval (Manager / Admin)**
- [ ] Approvals queue lists only entries from users in the Manager's team(s)
- [ ] Admin sees all pending entries
- [ ] Manager can approve an entry — Status moves to Approved
- [ ] Manager cannot approve entries outside their team
- [ ] Approving an entry triggers XP award (visible on the user's profile / dashboard)
- [ ] Level updates if the threshold is crossed
- [ ] Badge(s) award when earned, appearing on the user's badges page

**Leaderboard**
- [ ] Leaderboard loads and orders users by XP descending
- [ ] User's own row is highlighted (if AC requires)

**Badges**
- [ ] Badges page lists all 7 seeded badges
- [ ] Earned badges display the user's earned-at timestamp
- [ ] Unearned badges display in locked state
- [ ] Badge icons load from wwwroot/badges/

**General**
- [ ] No unexpected banners or error toasts on load
- [ ] Performance acceptable (no obvious slowness vs baseline)

---

**Overall Result:**
- [ ] PASS — Safe to proceed with feature testing
- [ ] FAIL — Issues found, do not proceed (log bugs below)

**Issues Found:**
- None / [list any]

**Notes:**
- [Any observations worth flagging]
