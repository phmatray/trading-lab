# Component Migration Map

**Feature**: Component Refactoring and Organization
**Date**: 2025-01-08
**Purpose**: Detailed migration instructions for all 51 components

---

## Migration Overview

This document provides step-by-step instructions for migrating all components to the new Tb-prefixed Atomic Design structure. Follow the order listed to minimize broken references.

**Total Components**: 51 → 44 (after consolidation)
**Estimated Time**: 6-8 hours (with testing at each checkpoint)

---

## Migration Order

1. **Atoms** (8 components) - Base layer, no dependencies
2. **Molecules** (9 components) - Depend on Atoms
3. **Organisms** (6 components) - Depend on Atoms + Molecules
4. **Features** (21 components) - Depend on all levels
5. **Pages** (2 page movements) - Update after all components migrated
6. **Import Consolidation** - After all components and pages
7. **Cleanup** - Delete duplicates and unused files

---

## Phase 1: Atoms Migration (8 components)

### 1.1 TbButton

**Current**: `Components/Atoms/Button.razor`
**Target**: `Components/Atoms/TbButton/TbButton.razor`
**Duplicate**: `Components/Shared/Button.razor` (DELETE)

**Steps**:
```bash
# 1. Create subfolder
mkdir -p src/TradingBot.Web/Components/Atoms/TbButton

# 2. Rename component
mv src/TradingBot.Web/Components/Atoms/Button.razor \
   src/TradingBot.Web/Components/Atoms/TbButton/TbButton.razor

# 3. Move supporting enum
mv src/TradingBot.Web/Models/ButtonVariant.cs \
   src/TradingBot.Web/Components/Atoms/TbButton/ButtonVariant.cs
```

**File Updates**:
- `TbButton.razor`: Update namespace to `TradingBot.Web.Components.Atoms`
- `TbButton.razor`: Update copyright header `file="TbButton.razor"`
- `ButtonVariant.cs`: Update namespace to `TradingBot.Web.Components.Atoms`
- `ButtonVariant.cs`: Update copyright header `file="ButtonVariant.cs"`

**Reference Updates**: Find/replace `<Button` → `<TbButton` in all `.razor` files

**Verification**:
```bash
dotnet build
dotnet test
```

---

### 1.2 TbInput

**Current**: `Components/Atoms/Input.razor`
**Target**: `Components/Atoms/TbInput.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Atoms/Input.razor \
   src/TradingBot.Web/Components/Atoms/TbInput.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Atoms`
- Update copyright header `file="TbInput.razor"`

**Reference Updates**: Find/replace `<Input` → `<TbInput` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 1.3 TbIcon

**Current**: `Components/Atoms/Icon.razor`
**Target**: `Components/Atoms/TbIcon/TbIcon.razor`

**Steps**:
```bash
mkdir -p src/TradingBot.Web/Components/Atoms/TbIcon

mv src/TradingBot.Web/Components/Atoms/Icon.razor \
   src/TradingBot.Web/Components/Atoms/TbIcon/TbIcon.razor

mv src/TradingBot.Web/Models/IconName.cs \
   src/TradingBot.Web/Components/Atoms/TbIcon/IconName.cs

mv src/TradingBot.Web/Models/IconVariant.cs \
   src/TradingBot.Web/Components/Atoms/TbIcon/IconVariant.cs
```

**File Updates**:
- `TbIcon.razor`: Update namespace and copyright header
- `IconName.cs`: Update namespace to `TradingBot.Web.Components.Atoms`
- `IconVariant.cs`: Update namespace to `TradingBot.Web.Components.Atoms`

**Reference Updates**: Find/replace `<Icon` → `<TbIcon` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 1.4 TbBadge

**Current**: `Components/Atoms/Badge.razor`
**Target**: `Components/Atoms/TbBadge/TbBadge.razor`

**Steps**:
```bash
mkdir -p src/TradingBot.Web/Components/Atoms/TbBadge

mv src/TradingBot.Web/Components/Atoms/Badge.razor \
   src/TradingBot.Web/Components/Atoms/TbBadge/TbBadge.razor

# Split BadgeEnums.cs into separate files
# Manual step: Extract BadgeVariant and BadgeSize from BadgeEnums.cs
# Create Components/Atoms/TbBadge/BadgeVariant.cs
# Create Components/Atoms/TbBadge/BadgeSize.cs
# Delete Models/BadgeEnums.cs
```

**File Updates**:
- `TbBadge.razor`: Update namespace and copyright header
- `BadgeVariant.cs`: Create with namespace `TradingBot.Web.Components.Atoms`
- `BadgeSize.cs`: Create with namespace `TradingBot.Web.Components.Atoms`

**Reference Updates**: Find/replace `<Badge` → `<TbBadge` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 1.5 TbLabel

**Current**: `Components/Atoms/Label.razor`
**Target**: `Components/Atoms/TbLabel/TbLabel.razor`

**Steps**:
```bash
mkdir -p src/TradingBot.Web/Components/Atoms/TbLabel

mv src/TradingBot.Web/Components/Atoms/Label.razor \
   src/TradingBot.Web/Components/Atoms/TbLabel/TbLabel.razor

mv src/TradingBot.Web/Models/LabelSize.cs \
   src/TradingBot.Web/Components/Atoms/TbLabel/LabelSize.cs
```

**File Updates**:
- `TbLabel.razor`: Update namespace and copyright header
- `LabelSize.cs`: Update namespace to `TradingBot.Web.Components.Atoms`

**Reference Updates**: Find/replace `<Label` → `<TbLabel` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 1.6 TbSelect

**Current**: `Components/Atoms/Select.razor`
**Target**: `Components/Atoms/TbSelect.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Atoms/Select.razor \
   src/TradingBot.Web/Components/Atoms/TbSelect.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Atoms`
- Update copyright header `file="TbSelect.razor"`

**Reference Updates**: Find/replace `<Select` → `<TbSelect` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 1.7 TbSpinner

**Current**: `Components/Atoms/Spinner.razor`
**Target**: `Components/Atoms/TbSpinner/TbSpinner.razor`

**Steps**:
```bash
mkdir -p src/TradingBot.Web/Components/Atoms/TbSpinner

mv src/TradingBot.Web/Components/Atoms/Spinner.razor \
   src/TradingBot.Web/Components/Atoms/TbSpinner/TbSpinner.razor

# Extract SpinnerSize from SpinnerEnums.cs
# Create Components/Atoms/TbSpinner/SpinnerSize.cs
# Delete Models/SpinnerEnums.cs
```

**File Updates**:
- `TbSpinner.razor`: Update namespace and copyright header
- `SpinnerSize.cs`: Create with namespace `TradingBot.Web.Components.Atoms`

**Reference Updates**: Find/replace `<Spinner` → `<TbSpinner` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 1.8 TbToggle

**Current**: `Components/Atoms/Toggle.razor`
**Target**: `Components/Atoms/TbToggle.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Atoms/Toggle.razor \
   src/TradingBot.Web/Components/Atoms/TbToggle.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Atoms`
- Update copyright header `file="TbToggle.razor"`

**Reference Updates**: Find/replace `<Toggle` → `<TbToggle` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

**CHECKPOINT 1**: All Atoms migrated
```bash
dotnet build
dotnet test
git commit -m "Refactor: Migrate Atoms to Tb prefix"
```

---

## Phase 2: Molecules Migration (9 components)

### 2.1 TbCard

**Current**: `Components/Shared/Card.razor` (in Shared, needs migration)
**Target**: `Components/Molecules/TbCard.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Shared/Card.razor \
   src/TradingBot.Web/Components/Molecules/TbCard.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Molecules`
- Update copyright header `file="TbCard.razor"`
- Update internal references: `<Button` → `<TbButton`, `<Icon` → `<TbIcon`

**Reference Updates**: Find/replace `<Card` → `<TbCard` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.2 TbModal

**Current**: `Components/Shared/Modal.razor`
**Target**: `Components/Molecules/TbModal.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Shared/Modal.razor \
   src/TradingBot.Web/Components/Molecules/TbModal.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Molecules`
- Update copyright header `file="TbModal.razor"`
- Update internal references to TbButton, TbIcon

**Reference Updates**: Find/replace `<Modal` → `<TbModal` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.3 TbTable

**Current**: `Components/Shared/Table.razor`
**Target**: `Components/Molecules/TbTable.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Shared/Table.razor \
   src/TradingBot.Web/Components/Molecules/TbTable.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Molecules`
- Update copyright header `file="TbTable.razor"`
- Update internal references to TbIcon, TbBadge

**Reference Updates**: Find/replace `<Table` → `<TbTable` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.4 TbFormField

**Current**: `Components/Molecules/FormField.razor`
**Target**: `Components/Molecules/TbFormField.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Molecules/FormField.razor \
   src/TradingBot.Web/Components/Molecules/TbFormField.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Molecules`
- Update copyright header `file="TbFormField.razor"`
- Update internal references to TbLabel, TbInput

**Reference Updates**: Find/replace `<FormField` → `<TbFormField` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.5 TbMenuItem

**Current**: `Components/Molecules/MenuItem.razor`
**Target**: `Components/Molecules/TbMenuItem.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Molecules/MenuItem.razor \
   src/TradingBot.Web/Components/Molecules/TbMenuItem.razor
```

**File Updates**:
- Update namespace and copyright header
- Update internal references to TbIcon, TbBadge

**Reference Updates**: Find/replace `<MenuItem` → `<TbMenuItem` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.6 TbToast

**Current**: `Components/Molecules/Toast.razor`
**Target**: `Components/Molecules/TbToast/TbToast.razor`

**Steps**:
```bash
mkdir -p src/TradingBot.Web/Components/Molecules/TbToast

mv src/TradingBot.Web/Components/Molecules/Toast.razor \
   src/TradingBot.Web/Components/Molecules/TbToast/TbToast.razor

mv src/TradingBot.Web/Models/ToastType.cs \
   src/TradingBot.Web/Components/Molecules/TbToast/ToastType.cs
```

**File Updates**:
- `TbToast.razor`: Update namespace and copyright header
- `ToastType.cs`: Update namespace to `TradingBot.Web.Components.Molecules`
- Update internal references to TbIcon, TbButton

**Reference Updates**: Find/replace `<Toast` → `<TbToast` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.7 TbPageHeader

**Current**: `Components/Molecules/PageHeader.razor`
**Target**: `Components/Molecules/TbPageHeader.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Molecules/PageHeader.razor \
   src/TradingBot.Web/Components/Molecules/TbPageHeader.razor
```

**File Updates**:
- Update namespace and copyright header
- Update internal references to TbButton, TbIcon

**Reference Updates**: Find/replace `<PageHeader` → `<TbPageHeader` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.8 TbInfoTooltip

**Current**: `Components/Molecules/InfoTooltip.razor`
**Target**: `Components/Molecules/TbInfoTooltip/TbInfoTooltip.razor`

**Steps**:
```bash
mkdir -p src/TradingBot.Web/Components/Molecules/TbInfoTooltip

mv src/TradingBot.Web/Components/Molecules/InfoTooltip.razor \
   src/TradingBot.Web/Components/Molecules/TbInfoTooltip/TbInfoTooltip.razor

mv src/TradingBot.Web/Models/TooltipPosition.cs \
   src/TradingBot.Web/Components/Molecules/TbInfoTooltip/TooltipPosition.cs
```

**File Updates**:
- `TbInfoTooltip.razor`: Update namespace and copyright header
- `TooltipPosition.cs`: Update namespace to `TradingBot.Web.Components.Molecules`
- Update internal references to TbIcon

**Reference Updates**: Find/replace `<InfoTooltip` → `<TbInfoTooltip` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

### 2.9 TbTablePagination

**Current**: `Components/Molecules/TablePagination.razor`
**Target**: `Components/Molecules/TbTablePagination.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Molecules/TablePagination.razor \
   src/TradingBot.Web/Components/Molecules/TbTablePagination.razor
```

**File Updates**:
- Update namespace and copyright header
- Update internal references to TbButton

**Reference Updates**: Find/replace `<TablePagination` → `<TbTablePagination` in all `.razor` files

**Verification**: `dotnet build && dotnet test`

---

**CHECKPOINT 2**: All Molecules migrated
```bash
dotnet build
dotnet test
git commit -m "Refactor: Migrate Molecules to Tb prefix"
```

---

## Phase 3: Organisms Migration (6 components)

### 3.1 TbNavigationSidebar (Consolidation Required)

**Current**:
- `Components/Organisms/NavigationSidebar.razor` (primary)
- `Components/Layout/NavMenu.razor` (duplicate functionality)

**Target**: `Components/Organisms/TbNavigationSidebar.razor`

**Steps**:
```bash
# 1. Rename primary
mv src/TradingBot.Web/Components/Organisms/NavigationSidebar.razor \
   src/TradingBot.Web/Components/Organisms/TbNavigationSidebar.razor

# 2. Merge NavMenu functionality into TbNavigationSidebar (manual)
# - Copy any unique features from NavMenu.razor
# - Delete Components/Layout/NavMenu.razor

# 3. Update MainLayout.razor to use TbNavigationSidebar
```

**File Updates**:
- `TbNavigationSidebar.razor`: Update namespace and copyright header
- `TbNavigationSidebar.razor`: Update internal references to TbMenuItem, TbIcon, TbBadge
- `MainLayout.razor`: Replace `<NavMenu` with `<TbNavigationSidebar`

**Reference Updates**: Find/replace `<NavigationSidebar` → `<TbNavigationSidebar`

**Verification**: `dotnet build && dotnet test && manual browser test`

---

### 3.2 TbToastContainer

**Current**: `Components/Shared/ToastContainer.razor`
**Target**: `Components/Organisms/TbToastContainer.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Shared/ToastContainer.razor \
   src/TradingBot.Web/Components/Organisms/TbToastContainer.razor
```

**File Updates**:
- Update namespace to `TradingBot.Web.Components.Organisms`
- Update copyright header
- Update internal references to TbToast

**Reference Updates**: Find/replace `<ToastContainer` → `<TbToastContainer`

**Verification**: `dotnet build && dotnet test`

---

### 3.3 TbErrorBoundary

**Current**: `Components/Shared/ErrorBoundary.razor`
**Target**: `Components/Organisms/TbErrorBoundary.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Shared/ErrorBoundary.razor \
   src/TradingBot.Web/Components/Organisms/TbErrorBoundary.razor
```

**File Updates**:
- Update namespace and copyright header
- Update internal references to TbCard, TbButton, TbIcon

**Reference Updates**: Find/replace `<ErrorBoundary` → `<TbErrorBoundary`

**Verification**: `dotnet build && dotnet test`

---

### 3.4 TbNotificationCenter

**Current**: `Components/Organisms/NotificationCenter.razor`
**Target**: `Components/Organisms/TbNotificationCenter.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Organisms/NotificationCenter.razor \
   src/TradingBot.Web/Components/Organisms/TbNotificationCenter.razor
```

**File Updates**:
- Update namespace and copyright header
- Update internal references to TbBadge, TbIcon, TbButton

**Reference Updates**: Find/replace `<NotificationCenter` → `<TbNotificationCenter`

**Verification**: `dotnet build && dotnet test`

---

### 3.5 TbSettingsForm

**Current**: `Components/Organisms/SettingsForm.razor`
**Target**: `Components/Organisms/TbSettingsForm.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Organisms/SettingsForm.razor \
   src/TradingBot.Web/Components/Organisms/TbSettingsForm.razor
```

**File Updates**:
- Update namespace and copyright header
- Update internal references to TbFormField, TbInput, TbSelect, TbToggle, TbButton, TbCard

**Reference Updates**: Find/replace `<SettingsForm` → `<TbSettingsForm`

**Verification**: `dotnet build && dotnet test`

---

### 3.6 TbThemeProvider

**Current**: `Components/Organisms/ThemeProvider.razor`
**Target**: `Components/Organisms/TbThemeProvider.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Organisms/ThemeProvider.razor \
   src/TradingBot.Web/Components/Organisms/TbThemeProvider.razor
```

**File Updates**:
- Update namespace and copyright header
- No internal component references (service-based)

**Reference Updates**: Find/replace `<ThemeProvider` → `<TbThemeProvider`

**Verification**: `dotnet build && dotnet test`

---

**CHECKPOINT 3**: All Organisms migrated
```bash
dotnet build
dotnet test
git commit -m "Refactor: Migrate Organisms to Tb prefix and consolidate NavMenu"
```

---

## Phase 4: Features Migration (21 components)

### 4.1 Dashboard Features (6 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Dashboard
```

**Components**:
1. `DashboardHeader.razor` → `Features/Dashboard/TbDashboardHeader.razor`
2. `AccountSummaryCard.razor` → `Features/Dashboard/TbAccountSummaryCard.razor`
3. `PerformanceMetricsCard.razor` → `Features/Dashboard/TbPerformanceMetricsCard.razor`
4. `ActiveStrategiesCard.razor` → `Features/Dashboard/TbActiveStrategiesCard.razor`
5. `RecentTradesCard.razor` → `Features/Dashboard/TbRecentTradesCard.razor`
6. `MarketOverviewCard.razor` → `Features/Dashboard/TbMarketOverviewCard.razor`

**Batch Migration**:
```bash
for file in DashboardHeader AccountSummaryCard PerformanceMetricsCard ActiveStrategiesCard RecentTradesCard MarketOverviewCard; do
  mv "src/TradingBot.Web/Components/Dashboard/${file}.razor" \
     "src/TradingBot.Web/Components/Features/Dashboard/Tb${file}.razor"
done
```

**File Updates** (for each):
- Update namespace to `TradingBot.Web.Components.Features.Dashboard`
- Update copyright header with new file name
- Update all internal component references to Tb-prefixed versions

**Reference Updates**: Update Pages/Dashboard.razor to use new component names

**Verification**: `dotnet build && dotnet test`

---

### 4.2 Portfolio Features (4 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Portfolio
```

**Components**:
1. `PortfolioSummary.razor` → `Features/Portfolio/TbPortfolioSummary.razor`
2. `PositionCard.razor` → `Features/Portfolio/TbPositionCard.razor`
3. `PortfolioChart.razor` → `Features/Portfolio/TbPortfolioChart.razor`
4. `AssetAllocationChart.razor` → `Features/Portfolio/TbAssetAllocationChart.razor`

**Batch Migration**:
```bash
for file in PortfolioSummary PositionCard PortfolioChart AssetAllocationChart; do
  mv "src/TradingBot.Web/Components/Portfolio/${file}.razor" \
     "src/TradingBot.Web/Components/Features/Portfolio/Tb${file}.razor"
done
```

**File Updates**: Same pattern as Dashboard
**Reference Updates**: Update Pages/Portfolio.razor

**Verification**: `dotnet build && dotnet test`

---

### 4.3 Strategy Features (2 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Strategy
```

**Components**:
1. `StrategyCard.razor` → `Features/Strategy/TbStrategyCard.razor`
2. `StrategyConfigForm.razor` → `Features/Strategy/TbStrategyConfigForm.razor`

**Batch Migration**:
```bash
for file in StrategyCard StrategyConfigForm; do
  mv "src/TradingBot.Web/Components/Strategy/${file}.razor" \
     "src/TradingBot.Web/Components/Features/Strategy/Tb${file}.razor"
done
```

**File Updates**: Same pattern
**Reference Updates**: Update Pages/Strategies.razor

**Verification**: `dotnet build && dotnet test`

---

### 4.4 Risk Features (2 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Risk
```

**Components**:
1. `RiskMetricsCard.razor` → `Features/Risk/TbRiskMetricsCard.razor`
2. `RiskLimitsForm.razor` → `Features/Risk/TbRiskLimitsForm.razor`

**Batch Migration**: Same pattern

**Verification**: `dotnet build && dotnet test`

---

### 4.5 Performance Features (2 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Performance
```

**Components**:
1. `EquityCurveChart.razor` → `Features/Performance/TbEquityCurveChart.razor`
2. `PerformanceStatsCard.razor` → `Features/Performance/TbPerformanceStatsCard.razor`

**Batch Migration**: Same pattern

**Verification**: `dotnet build && dotnet test`

---

### 4.6 Backtest Features (3 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Backtest
```

**Components**:
1. `BacktestConfigForm.razor` → `Features/Backtest/TbBacktestConfigForm.razor`
2. `BacktestResultsCard.razor` → `Features/Backtest/TbBacktestResultsCard.razor`
3. `BacktestChart.razor` → `Features/Backtest/TbBacktestChart.razor`

**Batch Migration**: Same pattern
**Reference Updates**: Update Pages/Backtest.razor

**Verification**: `dotnet build && dotnet test`

---

### 4.7 Charts Features (2 components)

Create feature folder:
```bash
mkdir -p src/TradingBot.Web/Components/Features/Charts
```

**Components**:
1. `CandlestickChart.razor` → `Features/Charts/TbCandlestickChart.razor`
2. `LineChart.razor` → `Features/Charts/TbLineChart.razor`

**Batch Migration**: Same pattern

**Verification**: `dotnet build && dotnet test`

---

**CHECKPOINT 4**: All Features migrated
```bash
dotnet build
dotnet test
git commit -m "Refactor: Migrate all Feature components to Tb prefix"
```

---

## Phase 5: Pages Migration (2 pages)

### 5.1 Move Settings Page

**Current**: `Components/Pages/Settings.razor`
**Target**: `Pages/Settings.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Pages/Settings.razor \
   src/TradingBot.Web/Pages/Settings.razor
```

**File Updates**:
- Update all component references to Tb-prefixed versions
- Update copyright header if needed

**Verification**: `dotnet build && dotnet test`

---

### 5.2 Move Help Page

**Current**: `Components/Pages/Help.razor`
**Target**: `Pages/Help.razor`

**Steps**:
```bash
mv src/TradingBot.Web/Components/Pages/Help.razor \
   src/TradingBot.Web/Pages/Help.razor
```

**File Updates**:
- Update all component references to Tb-prefixed versions

**Verification**: `dotnet build && dotnet test`

---

### 5.3 Update Existing Pages

Update all component references in:
- `Pages/Home.razor`
- `Pages/Dashboard.razor`
- `Pages/Portfolio.razor`
- `Pages/Strategies.razor`
- `Pages/Backtest.razor`
- `Pages/Settings.razor`
- `Pages/Help.razor`

**Verification**: `dotnet build && dotnet test && manual browser testing`

---

**CHECKPOINT 5**: All pages migrated
```bash
dotnet build
dotnet test
git commit -m "Refactor: Move pages and update all component references"
```

---

## Phase 6: Import Consolidation

### 6.1 Create Consolidated _Imports.razor

Create `Components/_Imports.razor`:
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

---

### 6.2 Delete Duplicate Imports

```bash
rm src/TradingBot.Web/Pages/_Imports.razor
```

---

### 6.3 Verify and Clean Unused Imports

```bash
dotnet build
# Use IDE to remove unused using statements
dotnet build
```

---

**CHECKPOINT 6**: Import consolidation complete
```bash
dotnet build
dotnet test
git commit -m "Refactor: Consolidate _Imports.razor files"
```

---

## Phase 7: Cleanup

### 7.1 Delete Shared Folder

```bash
# Verify folder is empty
ls src/TradingBot.Web/Components/Shared

# Delete folder
rm -rf src/TradingBot.Web/Components/Shared
```

---

### 7.2 Delete Components/Pages Folder

```bash
# Verify folder is empty
ls src/TradingBot.Web/Components/Pages

# Delete folder
rm -rf src/TradingBot.Web/Components/Pages
```

---

### 7.3 Delete Old Feature Folders

```bash
rm -rf src/TradingBot.Web/Components/Dashboard
rm -rf src/TradingBot.Web/Components/Portfolio
rm -rf src/TradingBot.Web/Components/Strategy
rm -rf src/TradingBot.Web/Components/Risk
rm -rf src/TradingBot.Web/Components/Performance
rm -rf src/TradingBot.Web/Components/Backtest
rm -rf src/TradingBot.Web/Components/Charts
```

---

### 7.4 Verify StyleCop Compliance

```bash
dotnet build /p:RunAnalyzers=true
# Should show 0 warnings
```

---

**FINAL CHECKPOINT**: Cleanup complete
```bash
dotnet build
dotnet test
git commit -m "Refactor: Clean up old folders and verify StyleCop compliance"
```

---

## Verification Checklist

- [ ] All 51 components migrated to Tb prefix
- [ ] 7 duplicate components removed
- [ ] All 21 feature components in Features/ folders
- [ ] 2 pages moved from Components/Pages to Pages/
- [ ] Single _Imports.razor at Components root
- [ ] All enum files co-located with components
- [ ] All namespaces updated correctly
- [ ] All copyright headers updated
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` all tests pass
- [ ] StyleCop shows 0 warnings
- [ ] Manual browser testing passes for all pages

---

## Rollback Instructions

If any checkpoint fails:
```bash
git reset --hard HEAD~1  # Roll back to previous commit
```

If you need to restart from scratch:
```bash
git reset --hard origin/004-component-refactor  # Reset to branch start
```

---

## Estimated Timeline

| Phase | Components | Estimated Time |
|-------|-----------|----------------|
| Phase 1: Atoms | 8 | 1.5 hours |
| Phase 2: Molecules | 9 | 1.5 hours |
| Phase 3: Organisms | 6 | 1 hour |
| Phase 4: Features | 21 | 2 hours |
| Phase 5: Pages | 7 | 30 minutes |
| Phase 6: Imports | - | 15 minutes |
| Phase 7: Cleanup | - | 15 minutes |
| **Total** | **51** | **6-7 hours** |

Add 1-2 hours for manual browser testing and verification.