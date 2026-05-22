---
name: data-design-template
description: The canonical design.md template for the Data Designer agent (phase 4). Provides all 8 section headers (ER diagram, entities, indexes, constraints, seed data, migration strategy, query patterns, open questions). Invoke ONLY when writing or rewriting design.md.
---

# Data Design Template

Use exactly these section headers. The Data Critic gates on this structure.

```markdown
# Data Design - <Project Name>

## 1. ER diagram (Mermaid)
\`\`\`mermaid
erDiagram
    USER ||--o{ ORDER : places
    ORDER ||--o{ ORDER_LINE : contains
    ...
\`\`\`

## 2. Entities (EF Core C# definitions)
One C# class per entity. Required parts:
- Primary key (Id / GUID / composite)
- Properties with C# types matching the data classification in spec §5.4
- Navigation properties (forward + back)
- Inline comments on non-obvious choices (decimal precision, soft-delete flag, RowVersion)

Use the `entity-modeler` sub-agent to draft this section. It produces the C# class blocks from the feature list + entity list.

## 3. Indexes
For each query the spec implies, list the supporting index:
| Query (from feature) | Index | Filter / sort |
|---|---|---|
| "Show my open orders" | (UserId, Status) | Status = Open |
| "Top 10 users by XP" | (XP desc) | covering: FirstName, LastName |

Use the `index-coverage-checker` mental model from `data-modeling-patterns` skill - every list / filter / sort in §3 of the UI/UX design implies an index.

## 4. Constraints
- **FK delete behaviours**: Cascade / Restrict / NoAction - explain any non-default choice
- **Cascade cycle check**: confirm no two FKs from the same table cascade-delete into the same target (SQL Server error)
- **Unique constraints**: list per entity
- **Check constraints**: list (or "none")
- **Soft delete strategy**: per-entity (Yes / No / global filter)
- **Concurrency tokens** (RowVersion): list entities where the spec implies concurrent editing

## 5. Seed data
Reference data the app needs at startup:
- Roles (from spec §5.4 + Identity)
- Lookup tables / enums materialised as data
- System users (e.g. "system" actor for audit log entries)
- Per-environment differences (dev seed includes test users; prod does not)

## 6. Migration strategy
- Initial migration name and command
- Per-feature migration cadence
- Data migration approach for non-trivial schema changes (separate script vs migration `Up` body)
- Down-migration discipline (write the inverse, or note "forward-only")

## 7. Query patterns
For the 3 hottest queries identified from the spec (or §3 indexes), sketch the LINQ / EF Core query and confirm the index covers it:

\`\`\`csharp
// "Show my open orders" - uses IX_Orders_UserId_Status
var openOrders = await _db.Orders
    .Where(o => o.UserId == userId && o.Status == OrderStatus.Open)
    .OrderByDescending(o => o.PlacedAt)
    .ToListAsync();
\`\`\`

## 8. Open questions for Backend Dev
- <items the backend needs to resolve based on this data design>
- <any aggregation / view that depends on later decisions>

## 9. Changelog (iteration deltas)
- Iteration 1: initial design
- Iteration 2 (if any): <what changed and why>
```

## Section-numbering invariants
- §1: ER diagram (Mermaid)
- §2: entities (C# classes)
- §3: indexes
- §4: constraints (FKs, uniques, checks, soft-delete, concurrency)
- §5: seed data
- §6: migration strategy
- §7: query patterns
- §8: open questions for Backend Dev
- §9: changelog (iteration ≥ 2 only)

The Data Critic gates on:
- Every feature in spec.md is supported by entities + indexes
- Cascade cycle is checked
- Money columns use `decimal(18,2)` (or equivalent precision)
- PII / sensitive fields per spec §5.4 are flagged (hashed / encrypted as appropriate)
