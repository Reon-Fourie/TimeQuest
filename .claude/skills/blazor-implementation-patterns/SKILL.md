---
name: blazor-implementation-patterns
description: Blazor Web App (.NET 10) implementation patterns used by the Frontend Developer agent (phase 6). Covers component organization, render-mode discipline, dependency injection, async / cancellation, forms with EditForm + DataAnnotationsValidator, routing with NavLink, state management, error boundaries, accessibility wiring, bUnit test patterns. Invoke when implementing a component or answering critic findings about DTO reuse / render mode / accessibility.
---

# Blazor Implementation Patterns (Blazor Web App, .NET 10)

## File organization

```
Components/
  App.razor                 # Root document
  Routes.razor              # Router
  _Imports.razor            # @using directives shared by all components
  Layout/
    MainLayout.razor
    NavMenu.razor
  Pages/
    Orders/
      Index.razor           # /orders
      Place.razor           # /orders/new
      Detail.razor          # /orders/{Id:int}
    Admin/
      Users.razor           # /admin/users
  Shared/
    OrderCard.razor         # Reusable per UI/UX component inventory
    EmptyState.razor
    LoadingSkeleton.razor
    ErrorBanner.razor
    ConfirmDialog.razor
```

Page components live under `Components/Pages/<feature>/`. Reusable components live under `Components/Shared/`. The names match the UI/UX design's component inventory (§4).

## Render-mode discipline

The Architect chose a default (usually `InteractiveServer`). For each page, decide:

| Page kind | Render mode | Why |
|---|---|---|
| Landing / marketing | Static | SEO + fast first paint; no auth state |
| Login / register | InteractiveServer | Form interactivity, server-side validation |
| Auth'd app pages (default) | InteractiveServer | Project default |
| Real-time dashboards | InteractiveServer | SignalR circuit for push updates |
| Read-only public | InteractiveAuto or Static | Avoid circuit cost for anonymous traffic |

Set render mode at the top of the component (or globally in Routes.razor / App.razor):
```razor
@page "/orders"
@rendermode InteractiveServer
```

Override per-page only when the choice differs from the project default; document in summary.md.

## Dependency injection

Inject services via `@inject` directive in Razor files:
```razor
@inject OrderService Orders
@inject NavigationManager Nav
@inject ILogger<Index> Logger
```

For Interactive Server: services are scoped to the SignalR circuit (i.e. per user session). Do NOT inject DbContext directly into a component - always go through a backend service. The Frontend Critic blocks DbContext usage in components.

## DTO reuse contract (mandatory)

The backend exposes DTOs in a `Shared` (or `Contracts`) project. The frontend project references it:

```xml
<!-- TimeQuest.Web.csproj -->
<ItemGroup>
  <ProjectReference Include="..\TimeQuest.Shared\TimeQuest.Shared.csproj" />
</ItemGroup>
```

Use the DTOs directly:
```razor
@using TimeQuest.Shared.Dtos

<OrderCard Order="@order" />

@code {
    private List<OrderListItemDto> orders = new();
}
```

**Do NOT redefine DTOs frontend-side.** The Frontend Critic greps for class/record names in `Shared` that also appear in `Components/` and blocks on duplicates.

## Component patterns

### Standard data-loading page

```razor
@page "/orders"
@attribute [Authorize(Roles = "Customer")]
@rendermode InteractiveServer
@inject OrderService Orders
@inject ILogger<Index> Logger

<PageTitle>Your orders</PageTitle>

<h1>Your orders</h1>

@if (loading)
{
    <LoadingSkeleton RowCount="5" />
}
else if (error is not null)
{
    <ErrorBanner Message="@error" OnRetry="LoadAsync" />
}
else if (orders.Count == 0)
{
    <EmptyState
        Title="No orders yet"
        Message="Place your first order to see it here."
        CtaLabel="Place an order"
        CtaHref="/orders/new" />
}
else
{
    <ul aria-label="Your orders" class="list">
        @foreach (var o in orders)
        {
            <li><OrderCard Order="@o" /></li>
        }
    </ul>
}

@code {
    private bool loading = true;
    private string? error;
    private List<OrderListItemDto> orders = new();
    private CancellationTokenSource? cts;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        loading = true;
        error = null;
        try
        {
            orders = await Orders.GetForCurrentUserAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load orders");
            error = "We couldn't load your orders. Try again.";
        }
        finally
        {
            loading = false;
        }
    }

    public void Dispose() => cts?.Cancel();
}
```

The three states (empty/loading/error) are wired explicitly. The Frontend Critic blocks if any are missing on a data-loading component.

### Form page (EditForm + DataAnnotationsValidator)

```razor
@page "/orders/new"
@attribute [Authorize(Roles = "Customer")]
@rendermode InteractiveServer
@inject OrderService Orders
@inject NavigationManager Nav

<h1>Place an order</h1>

<EditForm Model="@model" OnValidSubmit="Submit" FormName="place-order">
    <DataAnnotationsValidator />
    <ValidationSummary aria-live="polite" />

    <div class="field">
        <label for="product">Product</label>
        <InputSelect id="product" @bind-Value="model.ProductId" aria-required="true">
            <option value="">Choose...</option>
            @foreach (var p in products) { <option value="@p.Id">@p.Name</option> }
        </InputSelect>
        <ValidationMessage For="@(() => model.ProductId)" />
    </div>

    <div class="field">
        <label for="qty">Quantity</label>
        <InputNumber id="qty" @bind-Value="model.Quantity" min="1" max="999" aria-required="true" />
        <ValidationMessage For="@(() => model.Quantity)" />
    </div>

    <button type="submit" disabled="@submitting">@(submitting ? "Submitting..." : "Place order")</button>

    @if (errorMessage is not null)
    {
        <ErrorBanner Message="@errorMessage" />
    }
</EditForm>

@code {
    private PlaceOrderRequest model = new(new());  // matches backend DTO
    private List<ProductDto> products = new();
    private bool submitting;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        products = await Orders.GetProductsAsync(default);
    }

    private async Task Submit()
    {
        submitting = true;
        errorMessage = null;
        try
        {
            var result = await Orders.PlaceOrderAsync(model, default);
            if (result.IsSuccess)
                Nav.NavigateTo($"/orders/{result.Value.Id}");
            else
                errorMessage = result.Error;
        }
        catch (Exception)
        {
            errorMessage = "Something went wrong. Try again.";
        }
        finally
        {
            submitting = false;
        }
    }
}
```

Notes:
- `EditForm` with `DataAnnotationsValidator` honours the backend DTO's `[Required]` / `[Range]` etc.
- `aria-live="polite"` on `ValidationSummary` announces errors.
- Buttons show a disabled / "Submitting..." state to prevent double submits.
- `<label for="...">` ALWAYS - never placeholder-only labels.

### Navigation

Use `<NavLink>` for in-app navigation - it handles active-state styling and accessibility:
```razor
<nav>
    <NavLink href="/orders" Match="NavLinkMatch.Prefix">My orders</NavLink>
    <NavLink href="/admin/users" Match="NavLinkMatch.All">Users</NavLink>
</nav>
```

Programmatic navigation uses `NavigationManager.NavigateTo(...)` - NOT `<a href>` and NOT raw JS.

### Role-based UI

Hide nav items the user can't use:
```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <NavLink href="/admin">Admin</NavLink>
    </Authorized>
</AuthorizeView>
```

For page-level protection, use `@attribute [Authorize(Roles = "...")]`. The router redirects anonymous users to login automatically.

## Async + cancellation

- Use `OnInitializedAsync` for initial load.
- Hold a `CancellationTokenSource` as a field; cancel it on Dispose.
- Pass the token to backend calls so the SignalR disconnect cleanly cancels in-flight work.
- Implement `IDisposable` or use `IAsyncDisposable`:
  ```razor
  @implements IDisposable
  ```

## State across components

For component-local state, fields are fine.

For state shared across components in one session, scoped DI service:
```csharp
public class CartState
{
    public List<CartLine> Lines { get; } = new();
    public event Action? OnChange;
    public void Add(CartLine l) { Lines.Add(l); OnChange?.Invoke(); }
}
```
Register `AddScoped<CartState>()` and inject where needed.

For server-side persisted state, that's a backend service - call through a normal `OrderService` or `UserService`.

## Error boundaries

Wrap risky areas (third-party rendering, complex async) in `<ErrorBoundary>`:
```razor
<ErrorBoundary>
    <ChildContent>
        <ComplexThing />
    </ChildContent>
    <ErrorContent Context="ex">
        <ErrorBanner Message="Something went wrong loading this section." />
    </ErrorContent>
</ErrorBoundary>
```

## Accessibility wiring

Match the WCAG AA baseline from the UI/UX `uiux-design-system` skill:

- Every `<input>` has a `<label for="...">`. Never placeholder-only.
- `aria-required="true"` on required inputs.
- `aria-describedby` linking inputs to their error messages.
- `aria-live="polite"` on `ValidationSummary`, status messages, toasts.
- Skip-to-content link in `MainLayout.razor`:
  ```razor
  <a class="skip-link" href="#main">Skip to content</a>
  <main id="main">
      @Body
  </main>
  ```
- Focus visible via `:focus-visible` CSS using the `--color-focus-ring` token.
- Modal dialogs: use `<dialog>` element OR trap focus with a JS interop helper; restore on close.

## bUnit tests

For component logic that matters (state transitions, conditional rendering, role gating), write a bUnit test:

```csharp
public class OrdersIndexTests : TestContext
{
    [Fact]
    public void Shows_empty_state_when_no_orders()
    {
        var stub = new OrderServiceStub(returns: new List<OrderListItemDto>());
        Services.AddSingleton<OrderService>(stub);

        var cut = RenderComponent<TimeQuest.Web.Components.Pages.Orders.Index>();

        cut.WaitForState(() => !stub.LoadingActive);

        Assert.Contains("No orders yet", cut.Markup);
    }
}
```

Keep bUnit tests focused on component behaviour, not styling. Full E2E goes to Playwright in phase 7.

## Build / test gate

Every iteration must end with:
- `dotnet build` succeeds with 0 errors
- bUnit tests (if any) pass

The Frontend Critic will run these.

## What NOT to do

- **No `HttpClient` to your own backend** when Interactive Server is in use - call services directly via DI.
- **No `DbContext` injected into components** - always through a service.
- **No DTO duplication** - reference the backend's `Shared` project.
- **No raw `<a href>` for internal nav** - use `<NavLink>` or `NavigationManager`.
- **No JS for behaviour you can do in C#** - JS interop is for browser-only things (focus, clipboard, etc.).
