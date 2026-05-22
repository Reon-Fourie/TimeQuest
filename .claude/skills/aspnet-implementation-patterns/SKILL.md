---
name: aspnet-implementation-patterns
description: ASP.NET / .NET 10 implementation patterns used by the Backend Developer agent (phase 5). Covers project structure conventions, service-class organization, minimal API endpoint patterns, DTO conventions, async / EF / validation / security defaults, authorization patterns, logging, and test patterns. Invoke when implementing a feature or answering critic findings about code quality / security.
---

# ASP.NET Implementation Patterns

## Project structure (default layout)

```
src/
  <Project>.Web/             # Blazor Web App + API endpoints (single project for ≤5 features)
  <Project>.Domain/          # Entities, value objects, domain interfaces
  <Project>.Application/     # Service classes, business logic
  <Project>.Infrastructure/  # EF Core DbContext, external integrations
  <Project>.Shared/          # DTOs reused by Web + Api + Frontend
tests/
  <Project>.UnitTests/       # Service tests (in-memory DB)
  <Project>.IntegrationTests/ # API + EF tests against real SQL (Testcontainers)
```

For ≤5 features and one team, collapse to:
```
TimeQuest/
  Models/        # Entities
  Data/          # DbContext
  Services/      # Business logic
  Endpoints/     # Minimal API or Razor pages
  Shared/        # DTOs
  Components/    # Blazor (handled by Frontend phase)
TimeQuest.Tests/
```

The Architect's design.md decides which.

## Service classes

One service per aggregate root (Order, User, Team). Inject dependencies via constructor. Methods are async-all-the-way.

```csharp
public class OrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken ct)
    {
        // 1. Validate (or rely on DataAnnotations / FluentValidation at boundary)
        // 2. Load required entities
        // 3. Apply business rules
        // 4. Persist
        // 5. Map to DTO
        // 6. Return Result.Success(dto) or Result.Failure("reason")
    }
}
```

Return type: prefer `Result<T>` / `Either<E, T>` patterns over throwing for expected business failures. Throw only for invariant violations.

## Minimal API endpoints (default)

Group endpoints per resource. One file per resource.

```csharp
public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .RequireAuthorization()
            .WithTags("Orders");

        group.MapGet("/", GetMine);
        group.MapPost("/", Place);
        group.MapGet("/{id:int}", GetById);

        return app;
    }

    private static async Task<Results<Ok<List<OrderListItemDto>>, UnauthorizedHttpResult>> GetMine(
        ClaimsPrincipal user,
        OrderService svc,
        CancellationToken ct)
    {
        var userId = user.GetUserId();  // extension method
        var list = await svc.GetForUserAsync(userId, ct);
        return TypedResults.Ok(list);
    }
}
```

Wire up in Program.cs:
```csharp
app.MapOrders();
app.MapUsers();
```

## DTOs

Place in `Shared` project so Frontend can reference. Use records for immutability:

```csharp
namespace <Project>.Shared.Dtos;

public record OrderDto(int Id, DateTime PlacedAt, decimal Total, string Status, List<OrderLineDto> Lines);
public record OrderLineDto(string ProductName, int Quantity, decimal UnitPrice);
public record OrderListItemDto(int Id, DateTime PlacedAt, decimal Total, string Status);
public record PlaceOrderRequest([Required] List<PlaceOrderLine> Lines);
public record PlaceOrderLine([Required] int ProductId, [Range(1, 999)] int Quantity);
```

Naming:
- `<Entity>Dto` for the full read model
- `<Entity>ListItemDto` for list projections
- `<Verb><Entity>Request` for command inputs
- `<Verb><Entity>Response` for command outputs (often the same as `Dto`)

## Mapping

Hand-written mapping methods on the service or extension methods on the entity. Don't pull in AutoMapper unless the spec needs heavy mapping - it adds reflection overhead and is a frequent source of bugs.

```csharp
private static OrderDto ToDto(Order o) => new(
    o.Id,
    o.PlacedAt,
    o.Total,
    o.Status.ToString(),
    o.Lines.Select(l => new OrderLineDto(l.Product.Name, l.Quantity, l.UnitPrice)).ToList()
);
```

## Async discipline

- Async all the way down. No `.Result` / `.Wait()`. EVER.
- Pass `CancellationToken` from endpoint -> service -> EF call.
- For fire-and-forget work, use `IHostedService` or a queued background processor - not unawaited tasks.

```csharp
// BAD
var data = _db.Orders.ToList();  // sync over async - blocks thread

// GOOD
var data = await _db.Orders.ToListAsync(ct);
```

## EF Core query patterns

- Use LINQ; never string-concat into `FromSqlRaw`. If you need raw SQL, use `FromSqlInterpolated` (parameterised).
- Project to DTOs early in the query when the index supports it (avoids loading whole entity tree):
  ```csharp
  await _db.Orders
      .Where(o => o.UserId == userId)
      .Select(o => new OrderListItemDto(o.Id, o.PlacedAt, o.Total, o.Status.ToString()))
      .ToListAsync(ct);
  ```
- Use `AsNoTracking()` for read-only queries (avoids change-tracker overhead).
- For paged lists: `.Skip(skip).Take(take).ToListAsync(ct)`.
- Avoid N+1: include nav props you'll use (`Include`) OR project to DTO with sub-`Select`.

## Validation

At the API boundary:
1. **DataAnnotations on DTOs** - `[Required]`, `[Range]`, `[StringLength]`, `[EmailAddress]`. Free, declarative.
2. **`MiniValidation` or `IValidator<T>` (FluentValidation)** for complex rules - inject and validate manually before invoking service.

Don't re-validate in the service unless the validation depends on DB state (e.g. "email must be unique" - checked in service).

## Security defaults

- **All endpoints require auth by default**. Group-level `.RequireAuthorization()`; individual `[AllowAnonymous]` only for explicitly public.
- **Role / policy authorization**:
  ```csharp
  group.MapPost("/admin/users", AdminCreate)
      .RequireAuthorization("AdminOnly");

  // Program.cs
  builder.Services.AddAuthorizationBuilder()
      .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
  ```
- **Resource-based authorization** for "users can only see their own orders":
  ```csharp
  var order = await _db.Orders.FindAsync(id);
  if (order is null || order.UserId != user.GetUserId())
      return TypedResults.NotFound();  // 404 not 403, to avoid info leak
  ```
- **Never log secrets / passwords / tokens / PII**. Use scoped log context with safe fields only.
- **Connection strings via Key Vault references**, never appsettings.json.
- **Anti-forgery** enabled in `Program.cs` (`app.UseAntiforgery()`).
- **HTTPS only** in production; HSTS header enabled.

## Audit logging

For every destructive write OR auth-relevant event, append to the audit table. Use a `SaveChangesInterceptor` on `ApplicationDbContext` to centralize:

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var ctx = eventData.Context!;
        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                ctx.Add(new AuditEntry { ... });
            }
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

## Configuration

- `appsettings.json` for non-sensitive defaults.
- `appsettings.{Environment}.json` for env-specific overrides.
- Environment variables override files.
- Secrets via Key Vault references (`@Microsoft.KeyVault(...)`) in App Service - never plain in appsettings.

## Logging

- `ILogger<T>` injected, structured log statements:
  ```csharp
  _logger.LogInformation("Order {OrderId} placed by user {UserId} for {Total:C}", id, userId, total);
  ```
- Levels: `Trace` (dev only), `Debug` (dev only), `Information` (business events), `Warning` (recoverable issues), `Error` (failures), `Critical` (data loss / service down).
- Never log secrets, tokens, full PII. The skill `ba-nfr-elicitation` lists what spec §5.6 redacts.

## Test patterns (xUnit)

One test class per service:

```csharp
public class OrderServiceTests
{
    private static ApplicationDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var db = new ApplicationDbContext(opts);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task PlaceOrder_HappyPath_ReturnsOrderDto()
    {
        // Arrange
        using var db = NewDb();
        db.Products.Add(new Product { Id = 1, Name = "Widget", UnitPrice = 10 });
        await db.SaveChangesAsync();
        var svc = new OrderService(db, NullLogger<OrderService>.Instance);

        // Act
        var result = await svc.PlaceOrderAsync("user-1",
            new PlaceOrderRequest(new() { new(1, 2) }), default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Total);
    }

    [Fact]
    public async Task PlaceOrder_UnknownProduct_ReturnsFailure()
    {
        // ...
    }
}
```

Naming: `<Method>_<Condition>_<Expectation>`. SQLite in-memory is fine for service tests; only use Testcontainers when the test depends on SQL Server-specific behaviour (RowVersion, sequences, etc.).

## File organization rules

- One public class per file. File name = class name.
- Service files live in `Services/` (or `Application/Services/` for split layout).
- DTO files live in `Shared/Dtos/`; one record per file is overkill - group related DTOs in one file (e.g. `OrderDtos.cs`).
- Test files mirror source: `Services/OrderService.cs` -> `Tests/Services/OrderServiceTests.cs`.

## Build / test gate

Every iteration MUST end with:
- `dotnet build` succeeds with 0 errors
- `dotnet test` passes
- summary.md reflects reality

If the build fails, fix or document the failure honestly. The Backend Critic will run these commands and block on mismatch.
