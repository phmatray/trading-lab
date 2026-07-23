# Blazor Code-Behind Refactor

## Goal

Split each Blazor component that contains C# logic into two files: a `.razor`
file owning markup and routing/layout directives, and a `.razor.cs` partial
class owning all C# code (fields, parameters, lifecycle methods, event
handlers, dependency injection). This improves separation of concerns, keeps
markup readable, and makes the C# side easier to navigate and edit.

## Scope

In scope: the 10 components below. Each currently has an `@code { … }` block.

| Component | Path |
|---|---|
| `DashboardPage` | `TradyStrat/Features/Dashboard/DashboardPage.razor` |
| `HeroCapital` | `TradyStrat/Features/Dashboard/Components/HeroCapital.razor` |
| `GrowthChart` | `TradyStrat/Features/Dashboard/Components/GrowthChart.razor` |
| `PortfolioRail` | `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor` |
| `TodaysCallCard` | `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor` |
| `RefreshFab` | `TradyStrat/Features/Dashboard/Components/RefreshFab.razor` |
| `VaultMasthead` | `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor` |
| `TradesPage` | `TradyStrat/Features/Trades/TradesPage.razor` |
| `AddTradeDialog` | `TradyStrat/Features/Trades/Components/AddTradeDialog.razor` |
| `SettingsPage` | `TradyStrat/Features/Settings/SettingsPage.razor` |

Out of scope (markup-only, no `@code`): `Routes.razor`, `MainLayout.razor`,
`App.razor`, `_Imports.razor`. These remain unchanged.

## Split rules

### Stays in `.razor`

- Routing/layout directives: `@page`, `@layout`, `@rendermode`, `@inherits`
- Markup
- `<style>` blocks (where present, e.g. `DashboardPage.razor`)
- `@using` directives that are required for the **markup** to compile (e.g.
  `@using TradyStrat.Features.Dashboard.Components` so `<HeroCapital>` resolves)

### Moves to `.razor.cs`

- The entire `@code { … }` body: fields, properties, `[Parameter]`s,
  lifecycle methods, event handlers, helper methods
- `@inject Foo Bar` → `[Inject] private Foo Bar { get; set; } = default!;`
- `@using` directives that only the C# code uses become regular `using`
  statements at the top of the `.cs` file

### Partial class shape

```csharp
using Microsoft.AspNetCore.Components;
using <other namespaces used by the C# code>;

namespace TradyStrat.Features.<Area>;
// or .Components for nested components

public partial class <ComponentName> : ComponentBase
{
    [Inject] private SomeService SomeService { get; set; } = default!;

    [Parameter] public SomeType SomeParam { get; set; }

    private <fields, methods, …>
}
```

Notes:

- `: ComponentBase` is explicit on the partial class so the file compiles
  standalone in the IDE.
- `[Inject]` properties use `private` access with `get; set; = default!;` to
  satisfy nullable-reference-types without warnings.
- Namespace matches the folder layout (consistent with existing C# files).

### `<style>` blocks

`DashboardPage.razor` contains an inline `<style>` block. It stays in the
`.razor` file. CSS reorganization is out of scope for this refactor.

## Project file

No `.csproj` changes required. The .NET 9 SDK automatically nests
`<ComponentName>.razor.cs` under `<ComponentName>.razor` in IDEs that respect
SDK-implied `DependentUpon` (Visual Studio, Rider, VS Code with the C# Dev Kit).

## Verification

- `dotnet build` for the solution succeeds with no new warnings.
- `dotnet test` for `TradyStrat.Tests` passes.
- Manual smoke test: launch the app, visit `/`, `/trades`, `/settings`;
  confirm the dashboard renders, refresh works, the rerun-AI modal opens, the
  add-trade dialog works, and settings save without crash.

## Risks and mitigations

- **Risk:** `@using` directives in `.razor` are dropped accidentally,
  breaking markup that referenced types in those namespaces.
  **Mitigation:** When moving usings, decide per-using whether it is
  referenced by markup (keep) or only by C# (move). Build after each component
  to catch regressions early.
- **Risk:** `[Inject]` requires the service to be registered in DI; the
  existing `@inject` already does. No DI registration changes needed.
- **Risk:** Render mode (`@rendermode InteractiveServer`) is markup-side and
  stays where it is. Don't move it.
