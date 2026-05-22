---
name: bugs
description: Logs a TimeQuest bug as a GitHub Issue body - Title, Scenario, Environment (Local Dev by default), Browser, OS, Expected/Actual Results, Steps to Recreate (with URL and logged-in user/role), Workaround, and Linked Feature. Outputs a clean body ready to paste or pass to `gh issue create`.
---

Help the user log a TimeQuest bug as a GitHub Issue. Ask for each section if not provided, then output the completed issue body ready to paste OR pass to `gh issue create --title "…" --body "…"`.

Rules:
- Environment is Local Dev unless the user specifies otherwise.
- Always ask for the linked feature / spec.md section / parent issue unless the user explicitly says "no link".
- Ask for the developer to assign the bug to.
- Ask for browser name and version.
- Ask for OS.
- Ask for the logged-in user (email or role) as part of Steps to Recreate.
- Ask for workaround details — if none, output "None".
- Output must be clean and ready to paste — no extra commentary after the body.

---

**Title**
[Short description of issue — Headline]

**Scenario**
[Short description of what is being tested]

**Environment**
Local Dev

**Browser**
[Browser name + version]

**OS**
[macOS / Windows version]

**Expected Results**
- [List how the feature should work]

**Actual Results**
- [Summary of issue]
- Screenshot: [attached / see below]
- Video: [attached / N/A]

**Steps to Recreate**
- URL: [Starting URL, e.g. https://localhost:5001/timesheet]
- Logged In User: [Email / role]
1. [Step 1]
2. [Step 2]
3. [Continue as needed]

**Workaround**
[Workaround details, or "None"]

**Additional Details**
- Assigned to: [Developer who worked on the feature]
- Linked Feature / Issue: [spec.md feature name or GitHub issue #]
