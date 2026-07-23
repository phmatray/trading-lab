# Data Model: Component Hierarchy and Organization

**Feature**: Component Refactoring and Organization
**Date**: 2025-01-08
**Status**: Complete

## Overview

This document defines the complete component hierarchy for the refactored TradingBot.Web application. It categorizes all 51 components into Atomic Design levels (Atoms, Molecules, Organisms, Features) with clear dependencies, properties, and locations.

---

## Component Hierarchy Tree

```
TradingBot.Web.Components
│
├── Atoms (8 components)
│   ├── TbButton
│   ├── TbInput
│   ├── TbIcon
│   ├── TbBadge
│   ├── TbLabel
│   ├── TbSelect
│   ├── TbSpinner
│   └── TbToggle
│
├── Molecules (9 components)
│   ├── TbCard
│   ├── TbModal
│   ├── TbTable
│   ├── TbFormField
│   ├── TbMenuItem
│   ├── TbToast
│   ├── TbPageHeader
│   └── TbInfoTooltip
│
├── Organisms (6 components)
│   ├── TbNavigationSidebar
│   ├── TbToastContainer
│   ├── TbErrorBoundary
│   ├── TbNotificationCenter
│   ├── TbSettingsForm
│   └── TbThemeProvider
│
└── Features
    ├── Dashboard (6 components)
    │   ├── TbDashboardHeader
    │   ├── TbAccountSummaryCard
    │   ├── TbPerformanceMetricsCard
    │   ├── TbActiveStrategiesCard
    │   ├── TbRecentTradesCard
    │   └── TbMarketOverviewCard
    │
    ├── Portfolio (4 components)
    │   ├── TbPortfolioSummary
    │   ├── TbPositionCard
    │   ├── TbPortfolioChart
    │   └── TbAssetAllocationChart
    │
    ├── Strategy (2 components)
    │   ├── TbStrategyCard
    │   └── TbStrategyConfigForm
    │
    ├── Risk (2 components)
    │   ├── TbRiskMetricsCard
    │   └── TbRiskLimitsForm
    │
    ├── Performance (2 components)
    │   ├── TbEquityCurveChart
    │   └── TbPerformanceStatsCard
    │
    ├── Backtest (3 components)
    │   ├── TbBacktestConfigForm
    │   ├── TbBacktestResultsCard
    │   └── TbBacktestChart
    │
    └── Charts (2 components)
        ├── TbCandlestickChart
        └── TbLineChart
```

---

## Atomic Design Level: Atoms (8 Components)

### 1. TbButton

**Location**: `Components/Atoms/TbButton/TbButton.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Button.razor` (rename to TbButton)

**Purpose**: Single-purpose button element with variant styling

**Parameters**:
- `Variant` (ButtonVariant): Primary, Secondary, Danger, Ghost
- `Size` (ButtonSize?): Small, Medium, Large
- `Disabled` (bool): Whether button is disabled
- `Type` (string): "button", "submit", "reset"
- `OnClick` (EventCallback<MouseEventArgs>): Click handler
- `ChildContent` (RenderFragment): Button content

**Supporting Types**:
- `ButtonVariant.cs` (SmartEnum): Enum for button variants
- `ButtonSize.cs` (SmartEnum): Enum for button sizes

**Dependencies**: None (pure HTML `<button>`)

**Migration Notes**:
- Delete duplicate `Components/Shared/Button.razor`
- Create subfolder `TbButton/` for component and enums
- Move `Models/ButtonVariant.cs` to `Components/Atoms/TbButton/ButtonVariant.cs`

---

### 2. TbInput

**Location**: `Components/Atoms/TbInput.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Input.razor` (rename to TbInput)

**Purpose**: Text input field with validation support

**Parameters**:
- `Value` (string): Input value (two-way binding)
- `Placeholder` (string): Placeholder text
- `Type` (string): "text", "password", "email", "number"
- `Disabled` (bool): Whether input is disabled
- `Required` (bool): Whether input is required
- `OnValueChanged` (EventCallback<string>): Value changed handler

**Supporting Types**: None

**Dependencies**: None (pure HTML `<input>`)

**Migration Notes**: Simple rename, no duplicates

---

### 3. TbIcon

**Location**: `Components/Atoms/TbIcon/TbIcon.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Icon.razor` (rename to TbIcon)

**Purpose**: Icon display using Heroicons library

**Parameters**:
- `Name` (IconName): Icon identifier (Home, Settings, Chart, etc.)
- `Variant` (IconVariant): Solid, Outline
- `Size` (IconSize): Small, Medium, Large
- `Color` (string?): Tailwind color class

**Supporting Types**:
- `IconName.cs` (SmartEnum): Enum for icon identifiers
- `IconVariant.cs` (SmartEnum): Enum for icon styles
- `IconSize.cs` (SmartEnum): Enum for icon sizes

**Dependencies**: None (renders SVG)

**Migration Notes**:
- Create subfolder `TbIcon/` for component and enums
- Move `Models/IconName.cs`, `Models/IconVariant.cs` to `Components/Atoms/TbIcon/`

---

### 4. TbBadge

**Location**: `Components/Atoms/TbBadge/TbBadge.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Badge.razor` (rename to TbBadge)

**Purpose**: Small status indicator or label

**Parameters**:
- `Variant` (BadgeVariant): Success, Warning, Error, Info, Neutral
- `Size` (BadgeSize): Small, Medium
- `ChildContent` (RenderFragment): Badge content

**Supporting Types**:
- `BadgeVariant.cs` (SmartEnum): Enum for badge variants
- `BadgeSize.cs` (SmartEnum): Enum for badge sizes

**Dependencies**: None (pure HTML `<span>`)

**Migration Notes**:
- Create subfolder `TbBadge/` for component and enums
- Move `Models/BadgeEnums.cs` to `Components/Atoms/TbBadge/` and split into separate files

---

### 5. TbLabel

**Location**: `Components/Atoms/TbLabel/TbLabel.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Label.razor` (rename to TbLabel)

**Purpose**: Form label element with consistent styling

**Parameters**:
- `For` (string): ID of associated input
- `Required` (bool): Show required indicator
- `Size` (LabelSize): Small, Medium, Large
- `ChildContent` (RenderFragment): Label text

**Supporting Types**:
- `LabelSize.cs` (SmartEnum): Enum for label sizes

**Dependencies**: None (pure HTML `<label>`)

**Migration Notes**:
- Create subfolder `TbLabel/` for component and enum
- Move `Models/LabelSize.cs` to `Components/Atoms/TbLabel/LabelSize.cs`

---

### 6. TbSelect

**Location**: `Components/Atoms/TbSelect.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Select.razor` (rename to TbSelect)

**Purpose**: Dropdown select element

**Parameters**:
- `Value` (string): Selected value (two-way binding)
- `Options` (IEnumerable<SelectOption>): Available options
- `Placeholder` (string): Placeholder text
- `Disabled` (bool): Whether select is disabled
- `OnValueChanged` (EventCallback<string>): Value changed handler

**Supporting Types**: None (uses generic SelectOption class from Models)

**Dependencies**: None (pure HTML `<select>`)

**Migration Notes**: Simple rename, no duplicates

---

### 7. TbSpinner

**Location**: `Components/Atoms/TbSpinner/TbSpinner.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Spinner.razor` (rename to TbSpinner)

**Purpose**: Loading indicator

**Parameters**:
- `Size` (SpinnerSize): Small, Medium, Large
- `Color` (string?): Tailwind color class

**Supporting Types**:
- `SpinnerSize.cs` (SmartEnum): Enum for spinner sizes

**Dependencies**: None (pure CSS animation)

**Migration Notes**:
- Create subfolder `TbSpinner/` for component and enum
- Move `Models/SpinnerEnums.cs` to `Components/Atoms/TbSpinner/SpinnerSize.cs`

---

### 8. TbToggle

**Location**: `Components/Atoms/TbToggle.razor`
**Namespace**: `TradingBot.Web.Components.Atoms`
**Current Location**: `Components/Atoms/Toggle.razor` (rename to TbToggle)

**Purpose**: Toggle switch (checkbox alternative)

**Parameters**:
- `Value` (bool): Toggle state (two-way binding)
- `Disabled` (bool): Whether toggle is disabled
- `OnValueChanged` (EventCallback<bool>): Value changed handler
- `Label` (string?): Optional label text

**Supporting Types**: None

**Dependencies**: None (pure HTML with CSS)

**Migration Notes**: Simple rename, no duplicates

---

## Atomic Design Level: Molecules (9 Components)

### 1. TbCard

**Location**: `Components/Molecules/TbCard.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Shared/Card.razor` (migrate from Shared)

**Purpose**: Content container with header, body, and footer

**Parameters**:
- `Title` (string?): Card title
- `Subtitle` (string?): Card subtitle
- `HeaderContent` (RenderFragment?): Custom header content
- `BodyContent` (RenderFragment): Card body content
- `FooterContent` (RenderFragment?): Custom footer content
- `Actions` (RenderFragment?): Action buttons in footer

**Dependencies**:
- TbButton (for footer actions)
- TbIcon (for header icons)

**Migration Notes**:
- Migrate from `Components/Shared/Card.razor` to `Components/Molecules/TbCard.razor`
- Update all page references to use TbCard

---

### 2. TbModal

**Location**: `Components/Molecules/TbModal.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Shared/Modal.razor` (migrate from Shared)

**Purpose**: Modal dialog overlay

**Parameters**:
- `IsOpen` (bool): Whether modal is visible (two-way binding)
- `Title` (string): Modal title
- `Size` (ModalSize): Small, Medium, Large, FullScreen
- `ShowCloseButton` (bool): Show X button
- `ChildContent` (RenderFragment): Modal content
- `FooterContent` (RenderFragment?): Modal footer actions
- `OnClose` (EventCallback): Close handler

**Dependencies**:
- TbButton (for close button and footer actions)
- TbIcon (for close icon)

**Migration Notes**:
- Migrate from `Components/Shared/Modal.razor` to `Components/Molecules/TbModal.razor`
- Update all component references to use TbModal

---

### 3. TbTable

**Location**: `Components/Molecules/TbTable.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Shared/Table.razor` (migrate from Shared)

**Purpose**: Data table with sorting and filtering

**Parameters**:
- `Columns` (IEnumerable<TableColumn>): Column definitions
- `Data` (IEnumerable<T>): Table data
- `Sortable` (bool): Enable sorting
- `Striped` (bool): Striped rows
- `Hoverable` (bool): Hover effect
- `OnRowClick` (EventCallback<T>): Row click handler

**Dependencies**:
- TbIcon (for sort indicators)
- TbBadge (for status columns)

**Migration Notes**:
- Migrate from `Components/Shared/Table.razor` to `Components/Molecules/TbTable.razor`
- Update all page references to use TbTable

---

### 4. TbFormField

**Location**: `Components/Molecules/TbFormField.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Molecules/FormField.razor` (rename to TbFormField)

**Purpose**: Form field with label, input, and validation

**Parameters**:
- `Label` (string): Field label
- `Required` (bool): Required field indicator
- `Error` (string?): Validation error message
- `HelpText` (string?): Helper text
- `ChildContent` (RenderFragment): Input element (TbInput, TbSelect, etc.)

**Dependencies**:
- TbLabel (for field label)
- TbInput/TbSelect (as child content)

**Migration Notes**: Simple rename, no duplicates

---

### 5. TbMenuItem

**Location**: `Components/Molecules/TbMenuItem.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Molecules/MenuItem.razor` (rename to TbMenuItem)

**Purpose**: Navigation menu item with icon and text

**Parameters**:
- `Icon` (IconName): Menu item icon
- `Text` (string): Menu item text
- `Href` (string): Navigation link
- `IsActive` (bool): Whether item is currently active
- `Badge` (string?): Optional badge text
- `OnClick` (EventCallback): Click handler

**Dependencies**:
- TbIcon (for menu icon)
- TbBadge (for optional badge)

**Migration Notes**: Simple rename, no duplicates

---

### 6. TbToast

**Location**: `Components/Molecules/TbToast/TbToast.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Molecules/Toast.razor` (rename to TbToast)

**Purpose**: Toast notification message

**Parameters**:
- `Type` (ToastType): Success, Warning, Error, Info
- `Title` (string): Toast title
- `Message` (string): Toast message
- `Duration` (int): Auto-dismiss duration (ms)
- `OnClose` (EventCallback): Close handler

**Supporting Types**:
- `ToastType.cs` (SmartEnum): Enum for toast types

**Dependencies**:
- TbIcon (for type icon)
- TbButton (for close button)

**Migration Notes**:
- Create subfolder `TbToast/` for component and enum
- Move `Models/ToastType.cs` to `Components/Molecules/TbToast/ToastType.cs`

---

### 7. TbPageHeader

**Location**: `Components/Molecules/TbPageHeader.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Molecules/PageHeader.razor` (rename to TbPageHeader)

**Purpose**: Page header with title, breadcrumbs, and actions

**Parameters**:
- `Title` (string): Page title
- `Subtitle` (string?): Page subtitle
- `Breadcrumbs` (IEnumerable<Breadcrumb>?): Breadcrumb navigation
- `Actions` (RenderFragment?): Header action buttons

**Dependencies**:
- TbButton (for action buttons)
- TbIcon (for breadcrumb separators)

**Migration Notes**: Simple rename, no duplicates

---

### 8. TbInfoTooltip

**Location**: `Components/Molecules/TbInfoTooltip/TbInfoTooltip.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Molecules/InfoTooltip.razor` (rename to TbInfoTooltip)

**Purpose**: Information icon with tooltip

**Parameters**:
- `Content` (string): Tooltip content
- `Position` (TooltipPosition): Top, Bottom, Left, Right

**Supporting Types**:
- `TooltipPosition.cs` (SmartEnum): Enum for tooltip positions

**Dependencies**:
- TbIcon (for info icon)

**Migration Notes**:
- Create subfolder `TbInfoTooltip/` for component and enum
- Move `Models/TooltipPosition.cs` to `Components/Molecules/TbInfoTooltip/TooltipPosition.cs`

---

### 9. TbTablePagination (New Discovery)

**Location**: `Components/Molecules/TbTablePagination.razor`
**Namespace**: `TradingBot.Web.Components.Molecules`
**Current Location**: `Components/Molecules/TablePagination.razor` (rename to TbTablePagination)

**Purpose**: Pagination controls for tables

**Parameters**:
- `CurrentPage` (int): Current page number
- `TotalPages` (int): Total number of pages
- `OnPageChanged` (EventCallback<int>): Page changed handler

**Dependencies**:
- TbButton (for page buttons)

**Migration Notes**: Simple rename, no duplicates

---

## Atomic Design Level: Organisms (6 Components)

### 1. TbNavigationSidebar

**Location**: `Components/Organisms/TbNavigationSidebar.razor`
**Namespace**: `TradingBot.Web.Components.Organisms`
**Current Location**: `Components/Organisms/NavigationSidebar.razor` (rename and consolidate with NavMenu)

**Purpose**: Application navigation sidebar

**Parameters**:
- `IsCollapsed` (bool): Whether sidebar is collapsed
- `OnToggle` (EventCallback): Collapse toggle handler

**Dependencies**:
- TbMenuItem (for each navigation item)
- TbIcon (for collapse button)
- TbBadge (for notification counts)

**Migration Notes**:
- Rename `Components/Organisms/NavigationSidebar.razor` to `TbNavigationSidebar.razor`
- **Consolidate** `Components/Layout/NavMenu.razor` into TbNavigationSidebar (duplicate functionality)
- Update MainLayout.razor to use TbNavigationSidebar

---

### 2. TbToastContainer

**Location**: `Components/Organisms/TbToastContainer.razor`
**Namespace**: `TradingBot.Web.Components.Organisms`
**Current Location**: `Components/Shared/ToastContainer.razor` (migrate from Shared)

**Purpose**: Container for displaying multiple toast notifications

**Parameters**:
- `Position` (ToastPosition): TopRight, TopLeft, BottomRight, BottomLeft

**Dependencies**:
- TbToast (renders multiple toasts)
- ToastService (injected service for managing toasts)

**Migration Notes**:
- Migrate from `Components/Shared/ToastContainer.razor` to `Components/Organisms/TbToastContainer.razor`
- Update MainLayout.razor reference

---

### 3. TbErrorBoundary

**Location**: `Components/Organisms/TbErrorBoundary.razor`
**Namespace**: `TradingBot.Web.Components.Organisms`
**Current Location**: `Components/Shared/ErrorBoundary.razor` (migrate from Shared)

**Purpose**: Error boundary for catching component exceptions

**Parameters**:
- `ChildContent` (RenderFragment): Content to wrap
- `ErrorContent` (RenderFragment<Exception>?): Custom error display

**Dependencies**:
- TbCard (for error display)
- TbButton (for retry action)
- TbIcon (for error icon)

**Migration Notes**:
- Migrate from `Components/Shared/ErrorBoundary.razor` to `Components/Organisms/TbErrorBoundary.razor`
- Update all page references

---

### 4. TbNotificationCenter

**Location**: `Components/Organisms/TbNotificationCenter.razor`
**Namespace**: `TradingBot.Web.Components.Organisms`
**Current Location**: `Components/Organisms/NotificationCenter.razor` (rename to TbNotificationCenter)

**Purpose**: Notification center dropdown

**Parameters**:
- `Notifications` (IEnumerable<Notification>): List of notifications
- `OnNotificationClick` (EventCallback<Notification>): Notification click handler
- `OnMarkAllRead` (EventCallback): Mark all as read handler

**Dependencies**:
- TbBadge (for unread count)
- TbIcon (for notification icon)
- TbButton (for mark all read)

**Migration Notes**: Simple rename, no duplicates

---

### 5. TbSettingsForm

**Location**: `Components/Organisms/TbSettingsForm.razor`
**Namespace**: `TradingBot.Web.Components.Organisms`
**Current Location**: `Components/Organisms/SettingsForm.razor` (rename to TbSettingsForm)

**Purpose**: Application settings form

**Parameters**:
- `Settings` (AppSettings): Current settings
- `OnSave` (EventCallback<AppSettings>): Save handler

**Dependencies**:
- TbFormField (for each setting)
- TbInput, TbSelect, TbToggle (for setting inputs)
- TbButton (for save/cancel actions)
- TbCard (for section grouping)

**Migration Notes**: Simple rename, no duplicates

---

### 6. TbThemeProvider

**Location**: `Components/Organisms/TbThemeProvider.razor`
**Namespace**: `TradingBot.Web.Components.Organisms`
**Current Location**: `Components/Organisms/ThemeProvider.razor` (rename to TbThemeProvider)

**Purpose**: Theme management and dark mode toggle

**Parameters**:
- `ChildContent` (RenderFragment): Content to theme

**Dependencies**:
- ThemeService (injected service)
- Browser localStorage (via JSInterop)

**Migration Notes**: Simple rename, no duplicates

---

## Atomic Design Level: Features (21 Components)

### Dashboard Feature (6 Components)

#### 1. TbDashboardHeader
**Location**: `Components/Features/Dashboard/TbDashboardHeader.razor`
**Current**: `Components/Dashboard/DashboardHeader.razor`
**Dependencies**: TbPageHeader, TbButton, TbSelect

#### 2. TbAccountSummaryCard
**Location**: `Components/Features/Dashboard/TbAccountSummaryCard.razor`
**Current**: `Components/Dashboard/AccountSummaryCard.razor`
**Dependencies**: TbCard, TbBadge, TbIcon

#### 3. TbPerformanceMetricsCard
**Location**: `Components/Features/Dashboard/TbPerformanceMetricsCard.razor`
**Current**: `Components/Dashboard/PerformanceMetricsCard.razor`
**Dependencies**: TbCard, TbBadge, TbIcon, TbSpinner

#### 4. TbActiveStrategiesCard
**Location**: `Components/Features/Dashboard/TbActiveStrategiesCard.razor`
**Current**: `Components/Dashboard/ActiveStrategiesCard.razor`
**Dependencies**: TbCard, TbTable, TbBadge

#### 5. TbRecentTradesCard
**Location**: `Components/Features/Dashboard/TbRecentTradesCard.razor`
**Current**: `Components/Dashboard/RecentTradesCard.razor`
**Dependencies**: TbCard, TbTable, TbBadge, TbTablePagination

#### 6. TbMarketOverviewCard
**Location**: `Components/Features/Dashboard/TbMarketOverviewCard.razor`
**Current**: `Components/Dashboard/MarketOverviewCard.razor`
**Dependencies**: TbCard, TbLineChart (from Charts feature)

---

### Portfolio Feature (4 Components)

#### 1. TbPortfolioSummary
**Location**: `Components/Features/Portfolio/TbPortfolioSummary.razor`
**Current**: `Components/Portfolio/PortfolioSummary.razor`
**Dependencies**: TbCard, TbBadge, TbIcon

#### 2. TbPositionCard
**Location**: `Components/Features/Portfolio/TbPositionCard.razor`
**Current**: `Components/Portfolio/PositionCard.razor`
**Dependencies**: TbCard, TbBadge, TbButton, TbModal

#### 3. TbPortfolioChart
**Location**: `Components/Features/Portfolio/TbPortfolioChart.razor`
**Current**: `Components/Portfolio/PortfolioChart.razor`
**Dependencies**: TbCard, TbLineChart, TbSelect

#### 4. TbAssetAllocationChart
**Location**: `Components/Features/Portfolio/TbAssetAllocationChart.razor`
**Current**: `Components/Portfolio/AssetAllocationChart.razor`
**Dependencies**: TbCard, ApexChart (third-party)

---

### Strategy Feature (2 Components)

#### 1. TbStrategyCard
**Location**: `Components/Features/Strategy/TbStrategyCard.razor`
**Current**: `Components/Strategy/StrategyCard.razor`
**Dependencies**: TbCard, TbBadge, TbButton, TbToggle

#### 2. TbStrategyConfigForm
**Location**: `Components/Features/Strategy/TbStrategyConfigForm.razor`
**Current**: `Components/Strategy/StrategyConfigForm.razor`
**Dependencies**: TbFormField, TbInput, TbSelect, TbButton, TbCard

---

### Risk Feature (2 Components)

#### 1. TbRiskMetricsCard
**Location**: `Components/Features/Risk/TbRiskMetricsCard.razor`
**Current**: `Components/Risk/RiskMetricsCard.razor`
**Dependencies**: TbCard, TbBadge, TbIcon, TbInfoTooltip

#### 2. TbRiskLimitsForm
**Location**: `Components/Features/Risk/TbRiskLimitsForm.razor`
**Current**: `Components/Risk/RiskLimitsForm.razor`
**Dependencies**: TbFormField, TbInput, TbToggle, TbButton, TbCard

---

### Performance Feature (2 Components)

#### 1. TbEquityCurveChart
**Location**: `Components/Features/Performance/TbEquityCurveChart.razor`
**Current**: `Components/Performance/EquityCurveChart.razor`
**Dependencies**: TbCard, TbLineChart, TbSelect

#### 2. TbPerformanceStatsCard
**Location**: `Components/Features/Performance/TbPerformanceStatsCard.razor`
**Current**: `Components/Performance/PerformanceStatsCard.razor`
**Dependencies**: TbCard, TbBadge, TbTable

---

### Backtest Feature (3 Components)

#### 1. TbBacktestConfigForm
**Location**: `Components/Features/Backtest/TbBacktestConfigForm.razor`
**Current**: `Components/Backtest/BacktestConfigForm.razor`
**Dependencies**: TbFormField, TbInput, TbSelect, TbButton, TbCard

#### 2. TbBacktestResultsCard
**Location**: `Components/Features/Backtest/TbBacktestResultsCard.razor`
**Current**: `Components/Backtest/BacktestResultsCard.razor`
**Dependencies**: TbCard, TbTable, TbBadge

#### 3. TbBacktestChart
**Location**: `Components/Features/Backtest/TbBacktestChart.razor`
**Current**: `Components/Backtest/BacktestChart.razor`
**Dependencies**: TbCard, TbCandlestickChart, TbSelect

---

### Charts Feature (2 Components)

#### 1. TbCandlestickChart
**Location**: `Components/Features/Charts/TbCandlestickChart.razor`
**Current**: `Components/Charts/CandlestickChart.razor`
**Dependencies**: ApexChart (third-party), TbSpinner

#### 2. TbLineChart
**Location**: `Components/Features/Charts/TbLineChart.razor`
**Current**: `Components/Charts/LineChart.razor`
**Dependencies**: ApexChart (third-party), TbSpinner

---

## Supporting Types Migration Map

| Current Location | New Location | Type | Component |
|-----------------|--------------|------|-----------|
| Models/ButtonVariant.cs | Components/Atoms/TbButton/ButtonVariant.cs | SmartEnum | TbButton |
| Models/ButtonSize.cs | Components/Atoms/TbButton/ButtonSize.cs | SmartEnum | TbButton |
| Models/IconName.cs | Components/Atoms/TbIcon/IconName.cs | SmartEnum | TbIcon |
| Models/IconVariant.cs | Components/Atoms/TbIcon/IconVariant.cs | SmartEnum | TbIcon |
| Models/IconSize.cs | Components/Atoms/TbIcon/IconSize.cs | SmartEnum | TbIcon |
| Models/BadgeEnums.cs | Components/Atoms/TbBadge/BadgeVariant.cs | SmartEnum | TbBadge |
| Models/BadgeEnums.cs | Components/Atoms/TbBadge/BadgeSize.cs | SmartEnum | TbBadge |
| Models/LabelSize.cs | Components/Atoms/TbLabel/LabelSize.cs | SmartEnum | TbLabel |
| Models/SpinnerEnums.cs | Components/Atoms/TbSpinner/SpinnerSize.cs | SmartEnum | TbSpinner |
| Models/ToastType.cs | Components/Molecules/TbToast/ToastType.cs | SmartEnum | TbToast |
| Models/TooltipPosition.cs | Components/Molecules/TbInfoTooltip/TooltipPosition.cs | SmartEnum | TbInfoTooltip |

**Cross-Cutting Types (Stay in Models/)**:
- Models/NotificationSeverity.cs (used by multiple features)
- Models/SelectOption.cs (generic type used by TbSelect)
- Models/TableColumn.cs (generic type used by TbTable)
- Models/Breadcrumb.cs (generic type used by TbPageHeader)

---

## Pages Migration Map

| Current Location | New Location | Route |
|-----------------|--------------|-------|
| Components/Pages/Settings.razor | Pages/Settings.razor | /settings |
| Components/Pages/Help.razor | Pages/Help.razor | /help |

**Existing Pages (No Change)**:
- Pages/Home.razor → /
- Pages/Dashboard.razor → /dashboard
- Pages/Portfolio.razor → /portfolio
- Pages/Strategies.razor → /strategies
- Pages/Backtest.razor → /backtest

---

## Duplicate Components Consolidation

| Duplicate | Canonical Version | Action |
|-----------|------------------|--------|
| Components/Shared/Button.razor | Components/Atoms/TbButton.razor | DELETE Shared/Button, use TbButton |
| Components/Layout/NavMenu.razor | Components/Organisms/TbNavigationSidebar.razor | CONSOLIDATE into TbNavigationSidebar |
| Components/Shared/Card.razor | Components/Molecules/TbCard.razor | MIGRATE Shared/Card to TbCard |
| Components/Shared/Modal.razor | Components/Molecules/TbModal.razor | MIGRATE Shared/Modal to TbModal |
| Components/Shared/Table.razor | Components/Molecules/TbTable.razor | MIGRATE Shared/Table to TbTable |
| Components/Shared/ToastContainer.razor | Components/Organisms/TbToastContainer.razor | MIGRATE to TbToastContainer |
| Components/Shared/ErrorBoundary.razor | Components/Organisms/TbErrorBoundary.razor | MIGRATE to TbErrorBoundary |

---

## Dependency Graph Validation

**Atoms (No Dependencies)**:
- ✅ TbButton → None
- ✅ TbInput → None
- ✅ TbIcon → None
- ✅ TbBadge → None
- ✅ TbLabel → None
- ✅ TbSelect → None
- ✅ TbSpinner → None
- ✅ TbToggle → None

**Molecules (Atoms Only)**:
- ✅ TbCard → TbButton, TbIcon
- ✅ TbModal → TbButton, TbIcon
- ✅ TbTable → TbIcon, TbBadge
- ✅ TbFormField → TbLabel, TbInput/TbSelect
- ✅ TbMenuItem → TbIcon, TbBadge
- ✅ TbToast → TbIcon, TbButton
- ✅ TbPageHeader → TbButton, TbIcon
- ✅ TbInfoTooltip → TbIcon
- ✅ TbTablePagination → TbButton

**Organisms (Atoms + Molecules)**:
- ✅ TbNavigationSidebar → TbMenuItem, TbIcon, TbBadge
- ✅ TbToastContainer → TbToast
- ✅ TbErrorBoundary → TbCard, TbButton, TbIcon
- ✅ TbNotificationCenter → TbBadge, TbIcon, TbButton
- ✅ TbSettingsForm → TbFormField, TbInput, TbSelect, TbToggle, TbButton, TbCard
- ✅ TbThemeProvider → None (service-based)

**Features (All Levels)**:
- ✅ All feature components → Atoms, Molecules, Organisms as needed

**No Circular Dependencies**: ✅ Verified

---

## Total Component Count

- **Atoms**: 8 components
- **Molecules**: 9 components
- **Organisms**: 6 components
- **Features**: 21 components (6 Dashboard + 4 Portfolio + 2 Strategy + 2 Risk + 2 Performance + 3 Backtest + 2 Charts)
- **Total**: **44 components** (after consolidating 7 duplicates from original 51)

**Deleted/Consolidated**:
- Shared/Button.razor (duplicate, deleted)
- Layout/NavMenu.razor (consolidated into TbNavigationSidebar)
- Shared/Card.razor (migrated to TbCard)
- Shared/Modal.razor (migrated to TbModal)
- Shared/Table.razor (migrated to TbTable)
- Shared/ToastContainer.razor (migrated to TbToastContainer)
- Shared/ErrorBoundary.razor (migrated to TbErrorBoundary)

---

## Next Steps

This data model is complete. Proceed to:
1. Generate contracts/component-migration-map.md with detailed migration steps
2. Generate contracts/namespace-mapping.md with namespace update rules
3. Generate quickstart.md with developer migration guide