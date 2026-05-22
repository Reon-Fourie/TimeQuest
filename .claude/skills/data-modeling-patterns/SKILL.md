---
name: data-modeling-patterns
description: EF Core 9 modeling patterns used by the Data Designer agent (phase 4). Covers entity conventions, FK / cascade strategies (including the SQL Server cycle problem), index patterns, column type rules (decimal money, DateOnly, RowVersion), soft-delete approaches, sensitive-data handling, and seed-data idioms. Invoke when picking modeling approaches or answering critic findings about indexes / constraints / data types.
---

# Data Modeling Patterns (EF Core 9 on SQL Server)

## Primary key conventions

| Type | When to use | Notes |
|---|---|---|
| `int` (identity) | Default for line-of-business entities | Smallest, fastest, easy to reference |
| `long` | High-volume tables expected to exceed 2B rows | Audit logs, event streams |
| `Guid` (sequential) | Distributed ID generation needed | Use `NEWSEQUENTIALID()` not random Guids - avoids index fragmentation |
| Composite | Join tables only (e.g. `UserTeam` -> (UserId, TeamId)) | Configure with `HasKey(x => new { x.A, x.B })` |
| `string` | Only when external identity (e.g. `ApplicationUser.Id` from Identity is `string`) | Match the external type |

## Foreign keys and cascade behaviour

### The SQL Server cycle problem
SQL Server forbids multiple cascade paths into the same table. If entity B has two FKs that both point to entity A and both cascade, EF migration fails with `Introducing FOREIGN KEY constraint ... may cause cycles or multiple cascade paths.`

**Rule**: when an entity has TWO or more FKs to the same target, ONE keeps `Cascade` and the others use `Restrict` or `NoAction`.

### Choosing the behaviour
| Behaviour | When to use |
|---|---|
| `Cascade` | Child rows are conceptually part of the parent (e.g. OrderLines under Order) |
| `Restrict` | Child rows are independent and parent shouldn't be deleted if children exist (e.g. TimeEntries -> User) |
| `NoAction` | Same as Restrict semantically; use to break SQL Server cycle warnings |
| `SetNull` | Child reference is optional and clearing it is meaningful |

### Standard configuration
```csharp
builder.Entity<TimeEntry>()
    .HasOne(t => t.User)
    .WithMany(u => u.TimeEntries)
    .HasForeignKey(t => t.UserId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<TimeEntry>()
    .HasOne(t => t.ReviewedBy)
    .WithMany(u => u.ReviewedEntries)
    .HasForeignKey(t => t.ReviewedById)
    .OnDelete(DeleteBehavior.NoAction);  // breaks the cycle
```

## Column types

| C# type | SQL Server | Configuration |
|---|---|---|
| `decimal` (money) | `decimal(18,2)` | `HasColumnType("decimal(18,2)")` |
| `decimal` (high precision) | `decimal(18,6)` for percentages, rates | Specify precision explicitly |
| `DateOnly` | `date` | Native in EF Core 9 |
| `TimeOnly` | `time` | Native in EF Core 9 |
| `DateTime` | `datetime2(7)` | Default; use UTC always |
| `DateTimeOffset` | `datetimeoffset(7)` | When TZ matters |
| `string` (PII) | `nvarchar(N)` with MaxLength | Set explicit length |
| `string` (free text) | `nvarchar(max)` | Reserve for long descriptions |
| `byte[]` (RowVersion) | `rowversion` | `[Timestamp]` attribute or fluent `IsRowVersion()` |
| `byte[]` (file) | `varbinary(max)` | Prefer Blob storage unless small + always-needed |

## Indexes

### When to create one
Every query that filters / sorts / joins on a column needs an index covering at minimum the filter columns. Sources of queries:
1. Acceptance criteria implying a list ("show my X", "top N by Y") -> index on filter + sort
2. UI/UX wireframes (§3) - list pages and detail pages imply queries
3. Background jobs that scan data

### Composite index column order
Put the most selective / most filtered column first. For "WHERE A = ? AND B = ?", index `(A, B)` is good if A is more selective.

### Covering indexes
For hot read queries, `INCLUDE` the non-filter columns to avoid key lookups:
```csharp
builder.Entity<User>()
    .HasIndex(u => u.XP)
    .IsDescending()
    .IncludeProperties(u => new { u.FirstName, u.LastName });
```

### Filtered indexes
For partial conditions like "active orders only":
```csharp
builder.Entity<Order>()
    .HasIndex(o => new { o.UserId, o.PlacedAt })
    .HasFilter("[Status] = 1");  // 1 = Open
```

### What NOT to index
- Columns rarely filtered
- Wide columns (long strings)
- Tables with very high write/read ratio

## Soft delete

Two approaches:

**1. Per-entity flag + global query filter** (best when soft delete is universal)
```csharp
public class Order
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

builder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
```

**2. Status enum that includes "Deleted"** (best when there's already a lifecycle)
```csharp
public enum OrderStatus { Open, Closed, Cancelled, Deleted }
```

Default: do NOT soft delete unless the spec demands audit history. Hard deletes are simpler and audit logs serve recovery.

## Concurrency

Add `RowVersion` to any entity the spec says is concurrently editable (e.g. ticket status updated by multiple managers):
```csharp
public class Ticket
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
```

EF Core throws `DbUpdateConcurrencyException` on conflict; backend services should map to a user-friendly retry.

## Sensitive data (per spec §5.4 data classification)

| Classification | Storage approach |
|---|---|
| Public | Plain |
| Internal | Plain, access controlled by app authz |
| Confidential | Plain in DB but TDE on (Azure SQL default) + access controlled |
| Restricted | Encrypted column (Always Encrypted) OR hashed (one-way) - NEVER stored plain |

**Passwords**: never stored - use ASP.NET Identity's password hash (PBKDF2/SHA-256 by default).

**Payment card data**: don't store full PAN unless PCI-DSS environment. Use a payment processor's token.

**PII at scale under GDPR / POPIA**: support "right to be forgotten" - either hard delete or fully redacted (replace with `[redacted]`) rather than soft delete.

**Audit fields**: standard pattern - `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`. Set via `SaveChangesInterceptor` to avoid forgetting.

## DbContext conventions

- Inherit `IdentityDbContext<ApplicationUser>` if Identity is in use
- DbSet properties: `public DbSet<Order> Orders => Set<Order>();` (init-only with `Set<T>()` avoids null warnings)
- Configuration: prefer attributes for simple cases (`[MaxLength]`, `[Required]`), fluent `OnModelCreating` for relationships and complex constraints
- Migrations live under `Data/Migrations/`

## Seed data idioms

**Identity roles** (in `Program.cs` or `SeedData.cs`):
```csharp
foreach (var role in new[] { "Admin", "Manager", "User" })
    if (!await roleManager.RoleExistsAsync(role))
        await roleManager.CreateAsync(new IdentityRole(role));
```

**Lookup tables**: seed via `HasData` in `OnModelCreating` so it's part of migrations:
```csharp
builder.Entity<Country>().HasData(
    new Country { Id = 1, Code = "ZA", Name = "South Africa" },
    new Country { Id = 2, Code = "GB", Name = "United Kingdom" });
```

**Per-environment seed**: read environment from `IConfiguration` in seed step; only add test users in `Development`.

## Migration discipline

- Name migrations descriptively: `AddOrderRejectionReason`, not `Update1`.
- For destructive migrations (drop column, change type), write an explicit data-migration step in the `Up` body.
- For additive migrations (new tables, new optional columns), forward-only is fine.
- Never edit a deployed migration. If wrong, write a new corrective one.

## "Every feature has data support" rule

Walk the spec's features. For each, ask:
1. What entities does it read?
2. What entities does it write?
3. What query shape does its acceptance criteria imply?
4. Is there an index covering that query?

If you can't answer #4 with a yes, either add the index OR flag the feature for the Backend Dev to denormalise (with reasoning) OR raise in §8 Open Questions.
