# Namespace Mapping

**Feature**: Component Refactoring and Organization
**Date**: 2025-01-08
**Purpose**: Complete namespace mapping for all components and supporting types

---

## Namespace Strategy

All components and supporting types will use hierarchical namespaces that reflect their folder structure in the Components directory.

**Base Namespace**: `TradingBot.Web.Components`

---

## Component Namespace Mappings

### Atoms

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbButton.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbInput.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbIcon.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbBadge.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbLabel.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbSelect.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbSpinner.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |
| TbToggle.razor | `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` |

**Note**: Atoms already had correct namespace, no changes needed (only renaming)

---

### Molecules

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbCard.razor | `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Molecules` |
| TbModal.razor | `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Molecules` |
| TbTable.razor | `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Molecules` |
| TbFormField.razor | `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` |
| TbMenuItem.razor | `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` |
| TbToast.razor | `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` |
| TbPageHeader.razor | `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` |
| TbInfoTooltip.razor | `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` |
| TbTablePagination.razor | `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` |

**Action Required**: Components migrated from Shared need namespace update from `TradingBot.Web.Components.Shared` → `TradingBot.Web.Components.Molecules`

---

### Organisms

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbNavigationSidebar.razor | `TradingBot.Web.Components.Organisms` | `TradingBot.Web.Components.Organisms` |
| TbToastContainer.razor | `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Organisms` |
| TbErrorBoundary.razor | `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Organisms` |
| TbNotificationCenter.razor | `TradingBot.Web.Components.Organisms` | `TradingBot.Web.Components.Organisms` |
| TbSettingsForm.razor | `TradingBot.Web.Components.Organisms` | `TradingBot.Web.Components.Organisms` |
| TbThemeProvider.razor | `TradingBot.Web.Components.Organisms` | `TradingBot.Web.Components.Organisms` |

**Action Required**: Components migrated from Shared need namespace update

---

### Features - Dashboard

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbDashboardHeader.razor | `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` |
| TbAccountSummaryCard.razor | `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` |
| TbPerformanceMetricsCard.razor | `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` |
| TbActiveStrategiesCard.razor | `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` |
| TbRecentTradesCard.razor | `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` |
| TbMarketOverviewCard.razor | `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` |

**Action Required**: All Dashboard components need namespace update to include `.Features`

---

### Features - Portfolio

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbPortfolioSummary.razor | `TradingBot.Web.Components.Portfolio` | `TradingBot.Web.Components.Features.Portfolio` |
| TbPositionCard.razor | `TradingBot.Web.Components.Portfolio` | `TradingBot.Web.Components.Features.Portfolio` |
| TbPortfolioChart.razor | `TradingBot.Web.Components.Portfolio` | `TradingBot.Web.Components.Features.Portfolio` |
| TbAssetAllocationChart.razor | `TradingBot.Web.Components.Portfolio` | `TradingBot.Web.Components.Features.Portfolio` |

**Action Required**: All Portfolio components need namespace update to include `.Features`

---

### Features - Strategy

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbStrategyCard.razor | `TradingBot.Web.Components.Strategy` | `TradingBot.Web.Components.Features.Strategy` |
| TbStrategyConfigForm.razor | `TradingBot.Web.Components.Strategy` | `TradingBot.Web.Components.Features.Strategy` |

**Action Required**: Strategy components need namespace update to include `.Features`

---

### Features - Risk

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbRiskMetricsCard.razor | `TradingBot.Web.Components.Risk` | `TradingBot.Web.Components.Features.Risk` |
| TbRiskLimitsForm.razor | `TradingBot.Web.Components.Risk` | `TradingBot.Web.Components.Features.Risk` |

**Action Required**: Risk components need namespace update to include `.Features`

---

### Features - Performance

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbEquityCurveChart.razor | `TradingBot.Web.Components.Performance` | `TradingBot.Web.Components.Features.Performance` |
| TbPerformanceStatsCard.razor | `TradingBot.Web.Components.Performance` | `TradingBot.Web.Components.Features.Performance` |

**Action Required**: Performance components need namespace update to include `.Features`

---

### Features - Backtest

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbBacktestConfigForm.razor | `TradingBot.Web.Components.Backtest` | `TradingBot.Web.Components.Features.Backtest` |
| TbBacktestResultsCard.razor | `TradingBot.Web.Components.Backtest` | `TradingBot.Web.Components.Features.Backtest` |
| TbBacktestChart.razor | `TradingBot.Web.Components.Backtest` | `TradingBot.Web.Components.Features.Backtest` |

**Action Required**: Backtest components need namespace update to include `.Features`

---

### Features - Charts

| Component | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| TbCandlestickChart.razor | `TradingBot.Web.Components.Charts` | `TradingBot.Web.Components.Features.Charts` |
| TbLineChart.razor | `TradingBot.Web.Components.Charts` | `TradingBot.Web.Components.Features.Charts` |

**Action Required**: Charts components need namespace update to include `.Features`

---

## Supporting Types Namespace Mappings

### Atom Supporting Types

| Type | Old Namespace | New Namespace | Component |
|------|--------------|---------------|-----------|
| ButtonVariant.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbButton |
| ButtonSize.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbButton |
| IconName.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbIcon |
| IconVariant.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbIcon |
| IconSize.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbIcon |
| BadgeVariant.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbBadge |
| BadgeSize.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbBadge |
| LabelSize.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbLabel |
| SpinnerSize.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | TbSpinner |

---

### Molecule Supporting Types

| Type | Old Namespace | New Namespace | Component |
|------|--------------|---------------|-----------|
| ToastType.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Molecules` | TbToast |
| TooltipPosition.cs | `TradingBot.Web.Models` | `TradingBot.Web.Components.Molecules` | TbInfoTooltip |

---

### Cross-Cutting Types (Remain in Models)

| Type | Namespace | Usage |
|------|-----------|-------|
| NotificationSeverity.cs | `TradingBot.Web.Models` | Used by multiple features |
| SelectOption.cs | `TradingBot.Web.Models` | Generic type for TbSelect |
| TableColumn.cs | `TradingBot.Web.Models` | Generic type for TbTable |
| Breadcrumb.cs | `TradingBot.Web.Models` | Generic type for TbPageHeader |
| AppSettings.cs | `TradingBot.Web.Models` | Application configuration |
| Notification.cs | `TradingBot.Web.Models` | Domain model |

---

## Namespace Update Template

For component files (`.razor`):
```razor
@namespace TradingBot.Web.Components.{Category}[.{Subcategory}]

@* Component markup *@

@code {
    // Component code
}
```

For C# files (`.cs`):
```csharp
// <copyright file="FileName.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Components.{Category}[.{Subcategory}];

public sealed class TypeName : SmartEnum<TypeName>
{
    // Implementation
}
```

---

## _Imports.razor Namespace Imports

**Single Consolidated File**: `Components/_Imports.razor`

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using TradingBot.Web
@using TradingBot.Web.Components
@using TradingBot.Web.Components.Atoms
@using TradingBot.Web.Components.Molecules
@using TradingBot.Web.Components.Organisms
@using TradingBot.Web.Components.Features.Dashboard
@using TradingBot.Web.Components.Features.Portfolio
@using TradingBot.Web.Components.Features.Strategy
@using TradingBot.Web.Components.Features.Risk
@using TradingBot.Web.Components.Features.Performance
@using TradingBot.Web.Components.Features.Backtest
@using TradingBot.Web.Components.Features.Charts
@using TradingBot.Web.Services
@using TradingBot.Core.Models
```

**Why This Works**:
- Blazor's import inheritance: Pages automatically inherit from Components/_Imports.razor
- All Tb-prefixed components are available without explicit `@using` in each page
- Supporting types (enums) are in component namespaces, so they're imported automatically

---

## Namespace Verification Script

Use this bash script to verify all namespaces are correct:

```bash
#!/bin/bash

# Check Atoms
echo "Checking Atoms..."
grep -r "@namespace TradingBot.Web.Components.Atoms" src/TradingBot.Web/Components/Atoms/

# Check Molecules
echo "Checking Molecules..."
grep -r "@namespace TradingBot.Web.Components.Molecules" src/TradingBot.Web/Components/Molecules/

# Check Organisms
echo "Checking Organisms..."
grep -r "@namespace TradingBot.Web.Components.Organisms" src/TradingBot.Web/Components/Organisms/

# Check Features
echo "Checking Features..."
grep -r "@namespace TradingBot.Web.Components.Features" src/TradingBot.Web/Components/Features/

# Check for old namespaces (should return nothing)
echo "Checking for old namespaces (should be empty)..."
grep -r "@namespace TradingBot.Web.Components.Shared" src/TradingBot.Web/Components/
grep -r "@namespace TradingBot.Web.Components.Dashboard" src/TradingBot.Web/Components/ | grep -v ".Features"
grep -r "@namespace TradingBot.Web.Components.Portfolio" src/TradingBot.Web/Components/ | grep -v ".Features"

# Check C# files for old Models namespace
echo "Checking C# files for old Models namespace (should be empty except cross-cutting types)..."
grep -r "namespace TradingBot.Web.Models" src/TradingBot.Web/Components/
```

---

## Find/Replace Patterns

### For Component Usage in Pages

| Old Pattern | New Pattern |
|------------|------------|
| `<Button` | `<TbButton` |
| `<Input` | `<TbInput` |
| `<Icon` | `<TbIcon` |
| `<Badge` | `<TbBadge` |
| `<Label` | `<TbLabel` |
| `<Select` | `<TbSelect` |
| `<Spinner` | `<TbSpinner` |
| `<Toggle` | `<TbToggle` |
| `<Card` | `<TbCard` |
| `<Modal` | `<TbModal` |
| `<Table` | `<TbTable` |
| `<FormField` | `<TbFormField` |
| `<MenuItem` | `<TbMenuItem` |
| `<Toast` | `<TbToast` |
| `<PageHeader` | `<TbPageHeader` |
| `<InfoTooltip` | `<TbInfoTooltip` |
| `<TablePagination` | `<TbTablePagination` |
| `<NavigationSidebar` | `<TbNavigationSidebar` |
| `<ToastContainer` | `<TbToastContainer` |
| `<ErrorBoundary` | `<TbErrorBoundary` |
| `<NotificationCenter` | `<TbNotificationCenter` |
| `<SettingsForm` | `<TbSettingsForm` |
| `<ThemeProvider` | `<TbThemeProvider` |

### For Enum Imports

When enums are moved, update import statements:

```csharp
// OLD
using TradingBot.Web.Models;

// NEW (if using ButtonVariant)
using TradingBot.Web.Components.Atoms;

// NEW (if using ToastType)
using TradingBot.Web.Components.Molecules;
```

---

## Namespace Conflict Resolution

### Potential Conflicts

| Scenario | Resolution |
|----------|-----------|
| Component name conflicts with built-in types | Tb prefix prevents this |
| Enum conflicts across features | Co-location ensures unique namespaces |
| Third-party library conflicts (ApexChart) | Use full namespace qualification if needed |

### Example: Qualifying Third-Party Components

If there's ever a conflict with ApexChart or other third-party components:

```razor
@* Explicitly qualify if needed *@
<ApexCharts.ApexChart ... />
<TbLineChart ... />
```

---

## Summary of Namespace Changes

| Old Namespace | New Namespace | Component Count | Action |
|--------------|---------------|-----------------|--------|
| `TradingBot.Web.Components.Atoms` | `TradingBot.Web.Components.Atoms` | 8 | Rename files only |
| `TradingBot.Web.Components.Molecules` | `TradingBot.Web.Components.Molecules` | 6 | Rename files only |
| `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Molecules` | 3 | Update namespace |
| `TradingBot.Web.Components.Shared` | `TradingBot.Web.Components.Organisms` | 3 | Update namespace |
| `TradingBot.Web.Components.Organisms` | `TradingBot.Web.Components.Organisms` | 3 | Rename files only |
| `TradingBot.Web.Components.Dashboard` | `TradingBot.Web.Components.Features.Dashboard` | 6 | Update namespace |
| `TradingBot.Web.Components.Portfolio` | `TradingBot.Web.Components.Features.Portfolio` | 4 | Update namespace |
| `TradingBot.Web.Components.Strategy` | `TradingBot.Web.Components.Features.Strategy` | 2 | Update namespace |
| `TradingBot.Web.Components.Risk` | `TradingBot.Web.Components.Features.Risk` | 2 | Update namespace |
| `TradingBot.Web.Components.Performance` | `TradingBot.Web.Components.Features.Performance` | 2 | Update namespace |
| `TradingBot.Web.Components.Backtest` | `TradingBot.Web.Components.Features.Backtest` | 3 | Update namespace |
| `TradingBot.Web.Components.Charts` | `TradingBot.Web.Components.Features.Charts` | 2 | Update namespace |
| `TradingBot.Web.Models` | `TradingBot.Web.Components.Atoms` | 9 enums | Move and update |
| `TradingBot.Web.Models` | `TradingBot.Web.Components.Molecules` | 2 enums | Move and update |

**Total Files Requiring Namespace Update**: 32 components + 11 enum files = **43 files**

---

## Post-Migration Validation

After completing all namespace updates:

1. **Build Verification**:
   ```bash
   dotnet build
   # Should succeed with 0 errors
   ```

2. **StyleCop Verification**:
   ```bash
   dotnet build /p:RunAnalyzers=true
   # Should show 0 warnings
   ```

3. **Test Verification**:
   ```bash
   dotnet test
   # All tests should pass
   ```

4. **Manual Namespace Check**:
   - Run the verification script above
   - Ensure no old namespace references remain

5. **IntelliSense Test**:
   - Open a page file in IDE
   - Type `<Tb` and verify autocomplete shows all components
   - Verify component tooltips show correct namespaces