# Quickstart: Component Refactoring Migration Guide

**Feature**: Component Refactoring and Organization
**Date**: 2025-01-08
**Audience**: Developers working on TradingBot.Web

---

## Overview

This guide provides a quick-start for understanding and working with the refactored component structure. After the refactoring, all custom components use a "Tb" prefix and follow Atomic Design principles.

**Duration**: 15 minutes to read
**Prerequisites**: Familiarity with Blazor Server and Tailwind CSS

---

## Quick Reference: Component Locations

### Finding Components by Type

**Atoms** (Basic UI elements):
```
Components/Atoms/
├── TbButton/          # Buttons with variants
├── TbInput.razor      # Text inputs
├── TbIcon/            # Heroicons
├── TbBadge/           # Status badges
├── TbLabel/           # Form labels
├── TbSelect.razor     # Dropdowns
├── TbSpinner/         # Loading indicators
└── TbToggle.razor     # Toggle switches
```

**Molecules** (Composite components):
```
Components/Molecules/
├── TbCard.razor            # Content containers
├── TbModal.razor           # Dialog overlays
├── TbTable.razor           # Data tables
├── TbFormField.razor       # Form fields with labels
├── TbMenuItem.razor        # Navigation items
├── TbToast/                # Toast notifications
├── TbPageHeader.razor      # Page headers
├── TbInfoTooltip/          # Info tooltips
└── TbTablePagination.razor # Table pagination
```

**Organisms** (Complex sections):
```
Components/Organisms/
├── TbNavigationSidebar.razor  # Main navigation
├── TbToastContainer.razor     # Toast manager
├── TbErrorBoundary.razor      # Error handling
├── TbNotificationCenter.razor # Notification dropdown
├── TbSettingsForm.razor       # Settings panel
└── TbThemeProvider.razor      # Theme management
```

**Features** (Domain-specific):
```
Components/Features/
├── Dashboard/         # 6 components
├── Portfolio/         # 4 components
├── Strategy/          # 2 components
├── Risk/              # 2 components
├── Performance/       # 2 components
├── Backtest/          # 3 components
└── Charts/            # 2 components
```

---

## Quick Start: Using Components

### 1. Component Imports (Automatic)

All Tb components are automatically available in pages and components thanks to the consolidated `_Imports.razor`:

```razor
@* No need to add @using statements! *@
@page "/example"

<TbButton Variant="ButtonVariant.Primary">Click Me</TbButton>
<TbCard Title="My Card">
    Content here
</TbCard>
```

### 2. Common Component Patterns

#### Button with Variant
```razor
<TbButton Variant="ButtonVariant.Primary" OnClick="HandleClick">
    Save Changes
</TbButton>

<TbButton Variant="ButtonVariant.Secondary">
    Cancel
</TbButton>
```

#### Card with Header and Footer
```razor
<TbCard Title="Account Summary">
    <BodyContent>
        <p>Your balance: $10,000</p>
    </BodyContent>
    <FooterContent>
        <TbButton Variant="ButtonVariant.Primary">View Details</TbButton>
    </FooterContent>
</TbCard>
```

#### Form Field with Validation
```razor
<TbFormField Label="Email" Required="true" Error="@emailError">
    <TbInput @bind-Value="email" Type="email" Placeholder="you@example.com" />
</TbFormField>
```

#### Modal Dialog
```razor
<TbModal @bind-IsOpen="showModal" Title="Confirm Action">
    <ChildContent>
        <p>Are you sure you want to proceed?</p>
    </ChildContent>
    <FooterContent>
        <TbButton Variant="ButtonVariant.Danger" OnClick="Confirm">Confirm</TbButton>
        <TbButton Variant="ButtonVariant.Secondary" OnClick="() => showModal = false">Cancel</TbButton>
    </FooterContent>
</TbModal>
```

#### Table with Data
```razor
<TbTable Columns="tableColumns" Data="trades" Sortable="true" Striped="true" />

@code {
    private IEnumerable<Trade> trades = ...;
    private IEnumerable<TableColumn> tableColumns = new[]
    {
        new TableColumn { Header = "Symbol", PropertyName = nameof(Trade.Symbol) },
        new TableColumn { Header = "Price", PropertyName = nameof(Trade.Price) }
    };
}
```

---

## Component Hierarchy Rules

### Dependency Rules (IMPORTANT)

**Rule 1**: Atoms depend on **nothing** (only HTML elements)
```razor
@* TbButton.razor - CORRECT *@
<button class="@GetClasses()">@ChildContent</button>

@* TbButton.razor - WRONG (don't use other components in atoms) *@
<button><TbIcon Name="IconName.Home" /></button>  @* ❌ *@
```

**Rule 2**: Molecules depend on **Atoms only**
```razor
@* TbCard.razor - CORRECT *@
<div class="card">
    <TbButton>Action</TbButton>  @* ✓ Atom dependency OK *@
</div>

@* TbCard.razor - WRONG *@
<div class="card">
    <TbModal>...</TbModal>  @* ❌ No molecule-to-molecule dependencies *@
</div>
```

**Rule 3**: Organisms depend on **Atoms + Molecules**
```razor
@* TbNavigationSidebar.razor - CORRECT *@
<nav>
    <TbMenuItem Icon="IconName.Home">Home</TbMenuItem>  @* ✓ Molecule *@
    <TbIcon Name="IconName.ChevronLeft" />              @* ✓ Atom *@
</nav>
```

**Rule 4**: Features depend on **any level**
```razor
@* TbDashboardHeader.razor - CORRECT *@
<div>
    <TbPageHeader Title="Dashboard" />                 @* ✓ Molecule *@
    <TbButton>Refresh</TbButton>                       @* ✓ Atom *@
    <TbPerformanceMetricsCard />                       @* ✓ Feature *@
</div>
```

---

## Finding Components: Decision Tree

**"I need a..."**

1. **Basic UI element** (button, input, icon)
   → Check `Components/Atoms/`
   → Examples: TbButton, TbInput, TbIcon, TbBadge

2. **Composite component** (form field, card, modal)
   → Check `Components/Molecules/`
   → Examples: TbCard, TbModal, TbFormField, TbTable

3. **Complex section** (navigation, error boundary, theme)
   → Check `Components/Organisms/`
   → Examples: TbNavigationSidebar, TbErrorBoundary, TbToastContainer

4. **Feature-specific component** (dashboard card, strategy form)
   → Check `Components/Features/{FeatureName}/`
   → Examples: TbDashboardHeader, TbPortfolioChart, TbStrategyCard

---

## Tailwind CSS in Components

### Using Utility Classes

All components use Tailwind utility classes directly:

```razor
<div class="flex items-center gap-4 p-6 bg-white rounded-lg shadow-md">
    <TbIcon Name="IconName.Check" Size="IconSize.Large" />
    <span class="text-lg font-semibold text-gray-900">Success!</span>
</div>
```

### Component Variants

Variants are implemented using conditional classes in code:

```csharp
@code {
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

    private string GetClasses() =>
        Variant.Value switch
        {
            1 => "bg-blue-600 text-white hover:bg-blue-700",  // Primary
            2 => "bg-gray-200 text-gray-800 hover:bg-gray-300",  // Secondary
            3 => "bg-red-600 text-white hover:bg-red-700",  // Danger
            _ => "bg-blue-600 text-white"
        };
}
```

### Responsive Design

Use Tailwind's responsive prefixes:

```razor
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
    <TbCard Title="Card 1">...</TbCard>
    <TbCard Title="Card 2">...</TbCard>
    <TbCard Title="Card 3">...</TbCard>
</div>
```

---

## Testing Components with bUnit

### Test Structure

All component tests inherit from `Bunit.TestContext`:

```csharp
using Xunit;
using Bunit;
using TradingBot.Web.Components.Atoms;

public class TbButtonTests : TestContext
{
    [Fact]
    public void TbButton_RendersWithPrimaryVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<TbButton>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Primary)
            .Add(p => p.ChildContent, "Click Me")
        );

        // Assert
        cut.MarkupMatches("<button class=\"...\">Click Me</button>");
    }
}
```

### Testing Components with Child Content

```csharp
[Fact]
public void TbCard_RendersWithChildContent()
{
    var cut = RenderComponent<TbCard>(parameters => parameters
        .Add(p => p.Title, "Test Card")
        .Add(p => p.BodyContent, builder => builder.AddMarkupContent(0, "<p>Content</p>"))
    );

    cut.Find("p").TextContent.ShouldBe("Content");
}
```

### Testing Event Handlers

```csharp
[Fact]
public void TbButton_TriggersOnClick()
{
    var clicked = false;
    var cut = RenderComponent<TbButton>(parameters => parameters
        .Add(p => p.OnClick, () => clicked = true)
    );

    cut.Find("button").Click();

    Assert.True(clicked);
}
```

---

## Enum Types and Namespaces

### Component-Specific Enums

Enums are co-located with their components:

```csharp
// ButtonVariant.cs is in Components/Atoms/TbButton/
namespace TradingBot.Web.Components.Atoms;

public sealed class ButtonVariant : SmartEnum<ButtonVariant>
{
    public static readonly ButtonVariant Primary = new(1, nameof(Primary));
    public static readonly ButtonVariant Secondary = new(2, nameof(Secondary));
    public static readonly ButtonVariant Danger = new(3, nameof(Danger));
    // ...
}
```

### Using Enums in Pages

Thanks to `_Imports.razor`, enums are automatically available:

```razor
@page "/example"

<TbButton Variant="ButtonVariant.Primary">Save</TbButton>
<TbBadge Variant="BadgeVariant.Success">Active</TbBadge>
<TbIcon Name="IconName.Home" Variant="IconVariant.Solid" />

@code {
    // No need to explicitly import ButtonVariant, BadgeVariant, or IconName!
}
```

---

## Common Patterns

### Page Layout Pattern

```razor
@page "/my-page"
@using TradingBot.Web.Components.Features.MyFeature

<TbPageHeader Title="My Page" Subtitle="Description">
    <Actions>
        <TbButton Variant="ButtonVariant.Primary" OnClick="HandleAction">
            New Item
        </TbButton>
    </Actions>
</TbPageHeader>

<div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
    <TbMyFeatureCard />
    <TbMyFeatureCard />
</div>

@code {
    private void HandleAction() { ... }
}
```

### Form Pattern

```razor
<TbCard Title="User Settings">
    <BodyContent>
        <TbFormField Label="Name" Required="true">
            <TbInput @bind-Value="name" Placeholder="Enter name" />
        </TbFormField>

        <TbFormField Label="Email" Required="true">
            <TbInput @bind-Value="email" Type="email" Placeholder="you@example.com" />
        </TbFormField>

        <TbFormField Label="Notifications">
            <TbToggle @bind-Value="enableNotifications" />
        </TbFormField>
    </BodyContent>
    <FooterContent>
        <TbButton Variant="ButtonVariant.Primary" OnClick="SaveSettings">Save</TbButton>
        <TbButton Variant="ButtonVariant.Secondary" OnClick="Cancel">Cancel</TbButton>
    </FooterContent>
</TbCard>
```

### Dashboard Grid Pattern

```razor
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
    <TbAccountSummaryCard />
    <TbPerformanceMetricsCard />
    <TbActiveStrategiesCard />
    <TbRecentTradesCard Class="col-span-full" />
</div>
```

---

## Troubleshooting

### Component Not Found

**Problem**: `TbButton` not recognized by IntelliSense

**Solution**:
1. Verify `Components/_Imports.razor` includes `@using TradingBot.Web.Components.Atoms`
2. Rebuild the project: `dotnet build`
3. Restart IDE if necessary

---

### Enum Type Not Found

**Problem**: `ButtonVariant.Primary` not recognized

**Solution**:
1. Verify enum is in correct namespace (`TradingBot.Web.Components.Atoms` for `ButtonVariant`)
2. Check `_Imports.razor` includes the component's namespace
3. If using outside a page/component, add explicit `using TradingBot.Web.Components.Atoms;`

---

### Circular Dependency Error

**Problem**: Build error about circular component references

**Solution**:
1. Check component hierarchy rules (Atoms → Molecules → Organisms → Features)
2. Ensure no molecule references another molecule
3. Ensure no atom references any other component

---

### StyleCop Warning: Copyright Header

**Problem**: StyleCop warning about incorrect copyright header

**Solution**:
```csharp
// <copyright file="TbButton.razor" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>
```
Ensure `file` attribute matches exact filename.

---

## Migration Checklist for New Components

When creating a new custom component:

- [ ] Use "Tb" prefix for component name
- [ ] Place in correct category folder (Atoms/Molecules/Organisms/Features)
- [ ] Use correct namespace (`TradingBot.Web.Components.{Category}[.{Subcategory}]`)
- [ ] Add copyright header with correct filename
- [ ] Follow dependency rules (Atoms → Molecules → Organisms → Features)
- [ ] Co-locate supporting enums in component subfolder
- [ ] Use Tailwind utility classes (no custom CSS)
- [ ] Create bUnit tests inheriting from `TestContext`
- [ ] Update `_Imports.razor` if adding new feature category

---

## Quick Command Reference

### Build & Test
```bash
# Build project
dotnet build

# Run tests
dotnet test

# Build with StyleCop
dotnet build /p:RunAnalyzers=true
```

### Finding Components
```bash
# Find all components
find src/TradingBot.Web/Components -name "Tb*.razor"

# Find components by category
ls src/TradingBot.Web/Components/Atoms
ls src/TradingBot.Web/Components/Molecules
ls src/TradingBot.Web/Components/Organisms
ls src/TradingBot.Web/Components/Features
```

### Finding Component Usage
```bash
# Find where TbButton is used
grep -r "<TbButton" src/TradingBot.Web/Pages

# Find where ButtonVariant is used
grep -r "ButtonVariant\." src/TradingBot.Web
```

---

## Next Steps

- **Browse Components**: Explore `Components/` folders to familiarize yourself with available components
- **Read Tests**: Check `tests/TradingBot.Web.Tests` for usage examples
- **Study Pages**: Look at existing pages (`Pages/Dashboard.razor`, etc.) for real-world patterns
- **Refer to Spec**: See `specs/004-component-refactor/spec.md` for complete requirements
- **Check Data Model**: See `data-model.md` for complete component hierarchy

---

## Getting Help

- **Component Hierarchy**: See `data-model.md`
- **Migration Details**: See `contracts/component-migration-map.md`
- **Namespace Details**: See `contracts/namespace-mapping.md`
- **Research & Best Practices**: See `research.md`

---

**Last Updated**: 2025-01-08
**Version**: 1.0.0