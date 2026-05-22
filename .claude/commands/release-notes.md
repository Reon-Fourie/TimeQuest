Help the user generate structured release notes for a TimeQuest release from a list of tested and signed-off features / issues. Ask for each section if not provided, then output the completed release notes ready to share or paste into a GitHub release.

RULES:
- Tester is LydiaB.
- Ask for the release version or sprint name.
- Ask for the list of items included (issue numbers and titles, or spec.md feature names).
- Ask for the release date.
- Group items by type: New Features, Improvements, Bug Fixes, Infrastructure / Technical.
- If the user does not specify a type for an item, infer it from the title / description.
- Only include items that have been tested and signed off — ask the user to confirm before including any.


**Release Notes — {Release Version / Sprint Name}**

**Release Date:** {date}
**Environment:** Local Dev → {target}
**Prepared by:** LydiaB
**Date Prepared:** {today's date}


**New Features**
- **#{id}** — {title}: {one-line summary of what was delivered}

**Improvements**
- **#{id}** — {title}: {one-line summary of what was improved}

**Bug Fixes**
- **#{id}** — {title}: {one-line summary of what was fixed}

**Infrastructure / Technical**
- **#{id}** — {title}: {one-line summary of technical change}


**Known Issues / Exclusions**
- {List anything that was NOT included in this release and why}
- None if all planned items are included

**Testing Summary**
- Total items tested: {count}
- Total bugs logged during testing: {count}
- Outstanding bugs (not blocking release): {count}
- Tested by: LydiaB


**Sign-off**
- [ ] All included items tested and passed
- [ ] No critical bugs outstanding
- [ ] Release approved
