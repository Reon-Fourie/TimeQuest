---
name: test-session
description: Documents a structured TimeQuest QA session - linked feature / GitHub issue, start/end time, scope, test cases executed with results, bugs logged (issue refs), blocked items, observations, and a PASS / PARTIAL / FAIL session result. Asks only for missing fields.
---

Help the user document a structured TimeQuest testing session. Ask for each section if not provided, then output the completed report ready to paste into a GitHub issue, PR comment, or share.

RULES:
- Environment is Local Dev unless the user specifies otherwise.
- Tester is LydiaB.
- Always ask for the linked feature / issue unless the user says "no link".
- Ask for start and end time of the session.
- Always ask for overall session result (Pass / Fail / Partial).
- If result is Fail or Partial, always ask for bugs logged (GitHub issue references).

---

**Test Session Report**

**Feature / Issue:** #{id} — {title}
**Environment:** Local Dev
**Tester:** LydiaB
**Date:** {today's date}
**Session Start:** {start time}
**Session End:** {end time}

---

**Scope of Testing**
- {What was tested — features, flows, endpoints covered in this session}

**Test Cases Executed**

| # | Test Case | Result | Notes |
|---|---|---|---|
| 1 | {test case description} | ✅ Pass / ❌ Fail / ⚠️ Partial | {notes} |

---

**Bugs Logged**

| Bug # | Title | Severity | Status |
|---|---|---|---|
| #{issue} | {title} | {severity if assigned} | Logged |

*(None if no bugs found)*

---

**Blocked / Unable to Test**
- {List anything that could not be tested and why — missing data, environment issue, dependency not ready}
- None if fully tested

**Observations & Notes**
- {Any behaviour worth flagging that is not a formal bug — performance, UX concerns, edge cases noticed}

---

**Session Result:**
- ✅ PASS — All test cases passed, no bugs logged
- ⚠️ PARTIAL — Some test cases passed, bugs logged or items blocked
- ❌ FAIL — Critical failures found, feature cannot be signed off
