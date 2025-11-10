# TradingBot Component Architecture

This directory contains all UI components for the TradingBot web application, organized using the **Atomic Design** methodology with a "Tb" (TradingBot) prefix for consistent naming.

## Directory Structure

```
Components/
├── Atoms/              # Basic building blocks (buttons, inputs, icons)
├── Molecules/          # Simple component groups (cards, forms, modals)
├── Organisms/          # Complex component sections (sidebars, forms, containers)
├── Features/           # Feature-specific components organized by domain
│   ├── Dashboard/      # Dashboard-specific components
│   ├── Portfolio/      # Portfolio management components
│   ├── Strategy/       # Strategy configuration components
│   ├── Risk/           # Risk management components
│   ├── Performance/    # Performance analytics components
│   ├── Backtest/       # Backtesting components
│   └── Charts/         # Chart visualization components
├── Layout/             # Application layout components
└── Pages/              # Routable page components
```

## Atomic Design Hierarchy

The component organization follows strict dependency rules based on Atomic Design:

### Atoms (Level 1)
**Location**: `Components/Atoms/`
**Description**: The smallest, most fundamental building blocks of the UI.

**Dependency Rules**:
- ✅ Can use: HTML elements, CSS, Blazor directives
- ❌ Cannot use: Any other Tb components (Molecules, Organisms, Features)

**Components**:
- `TbButton/` - Button component with variant support
- `TbInput` - Text input component
- `TbIcon/` - Icon component with multiple icon sets
- `TbBadge/` - Badge/label component
- `TbLabel/` - Form label component
- `TbSelect` - Dropdown select component
- `TbSpinner/` - Loading spinner component
- `TbToggle` - Toggle/switch component

### Molecules (Level 2)
**Location**: `Components/Molecules/`
**Description**: Simple groups of Atoms functioning together as a unit.

**Dependency Rules**:
- ✅ Can use: Atoms, HTML elements, CSS
- ❌ Cannot use: Organisms, Features

**Components**:
- `TbCard` - Card container with header/body/footer
- `TbModal` - Modal dialog component
- `TbTable` - Data table component
- `TbFormField` - Form field with label and input
- `TbMenuItem` - Navigation menu item
- `TbToast/` - Toast notification component
- `TbPageHeader` - Page header with title and actions
- `TbInfoTooltip/` - Information tooltip component
- `TbTablePagination` - Table pagination controls

### Organisms (Level 3)
**Location**: `Components/Organisms/`
**Description**: Complex components composed of Molecules and/or Atoms.

**Dependency Rules**:
- ✅ Can use: Molecules, Atoms, HTML elements, CSS
- ❌ Cannot use: Features (to maintain reusability)

**Components**:
- `TbNavigationSidebar` - Main application sidebar with navigation menu
- `TbToastContainer` - Container managing multiple toast notifications
- `TbErrorBoundary` - Error boundary for graceful error handling
- `TbNotificationCenter` - Notification center dropdown
- `TbSettingsForm` - Application settings form
- `TbThemeProvider` - Theme switching and management

### Features (Level 4)
**Location**: `Components/Features/{Domain}/`
**Description**: Feature-specific components organized by business domain.

**Dependency Rules**:
- ✅ Can use: Organisms, Molecules, Atoms, HTML elements, CSS
- ✅ Can reference other domain components when necessary
- ✅ Should be cohesive - all components for a feature in one folder

**Domain Folders**:
- **Dashboard/**: Components for the main dashboard view
  - `TbDashboardHeader`, `TbAccountSummaryCard`, `TbPerformanceMetricsCard`,
    `TbActiveStrategiesCard`, `TbRecentTradesCard`, `TbMarketOverviewCard`

- **Portfolio/**: Components for portfolio management
  - `TbPortfolioSummary`, `TbPositionCard`, `TbPortfolioChart`, `TbAssetAllocationChart`

- **Strategy/**: Components for strategy configuration
  - `TbStrategyCard`, `TbStrategyConfigForm`

- **Risk/**: Components for risk management
  - `TbRiskMetricsCard`, `TbRiskLimitsForm`

- **Performance/**: Components for performance analytics
  - `TbEquityCurveChart`, `TbPerformanceStatsCard`

- **Backtest/**: Components for backtesting
  - `TbBacktestConfigForm`, `TbBacktestResultsCard`, `TbBacktestChart`

- **Charts/**: Reusable chart components
  - `TbCandlestickChart`, `TbLineChart`

## Naming Conventions

### Component Names
- All components are prefixed with `Tb` (TradingBot)
- Use PascalCase: `TbButton`, `TbCard`, `TbDashboardHeader`
- Descriptive names that indicate purpose: `TbAccountSummaryCard` not `TbAsc`

### File Organization
- **Simple components**: Single file `TbComponentName.razor`
- **Complex components**: Subfolder with supporting files
  ```
  TbButton/
  ├── TbButton.razor        # Component markup and logic
  ├── ButtonVariant.cs      # Supporting enum/class
  └── ButtonSize.cs         # Supporting enum/class
  ```

### Namespaces
Components follow a hierarchical namespace structure:
- Atoms: `TradingBot.Web.Components.Atoms`
- Molecules: `TradingBot.Web.Components.Molecules`
- Organisms: `TradingBot.Web.Components.Organisms`
- Features: `TradingBot.Web.Components.Features.{Domain}`
  - Example: `TradingBot.Web.Components.Features.Dashboard`

## Best Practices

### Component Development

1. **Start with Atoms**: Build or reuse existing atoms before creating new higher-level components
2. **Composition over Duplication**: Reuse existing components rather than duplicating functionality
3. **Single Responsibility**: Each component should do one thing well
4. **Co-locate Supporting Types**: Keep enums, models, and helpers with their components

### Dependency Management

1. **Respect the Hierarchy**: Never reference components from higher levels
   - ❌ Atoms cannot use Molecules
   - ❌ Molecules cannot use Organisms
   - ❌ Organisms cannot use Features

2. **Verify Dependencies**: Use the verification script to check for violations:
   ```bash
   # Check Atoms don't use Molecules/Organisms/Features
   grep -r "Molecules\|Organisms\|Features" src/TradingBot.Web/Components/Atoms/

   # Check Molecules don't use Organisms/Features
   grep -r "Organisms\|Features" src/TradingBot.Web/Components/Molecules/

   # Check Organisms don't use Features
   grep -r "Features" src/TradingBot.Web/Components/Organisms/
   ```

3. **Build Validation**: The build process will fail if circular dependencies exist

### Adding New Components

**Step 1**: Determine the correct level
- Is it a basic UI element? → Atom
- Is it a simple combination of atoms? → Molecule
- Is it a complex, reusable section? → Organism
- Is it specific to a business feature? → Feature

**Step 2**: Choose the location
- Atoms/Molecules/Organisms → Place in appropriate folder
- Features → Place in domain subfolder (Dashboard, Portfolio, etc.)

**Step 3**: Follow naming conventions
- Prefix with `Tb`
- Use descriptive PascalCase names
- Create subfolder if component has supporting files

**Step 4**: Set correct namespace
```csharp
@namespace TradingBot.Web.Components.{Level}
// or
@namespace TradingBot.Web.Components.Features.{Domain}
```

**Step 5**: Update copyright header
```csharp
// <copyright file="TbYourComponent.razor" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>
```

**Step 6**: Verify build and tests
```bash
dotnet build
dotnet test
```

## Import Management

The component architecture uses hierarchical `_Imports.razor` files:

- **Root Level** (`src/TradingBot.Web/_Imports.razor`):
  - Contains all common namespaces
  - Inherited by all pages and components

- **Components Level** (`Components/_Imports.razor`):
  - Can add component-specific imports if needed
  - Inherits from root level

Pages automatically inherit all component namespaces, so you can use any `Tb` component without additional `@using` directives.

## Verification Checklist

Before committing component changes, verify:

- [ ] Component follows Atomic Design hierarchy (correct level)
- [ ] Component name has `Tb` prefix
- [ ] Component is in correct folder/namespace
- [ ] Supporting types are co-located (not in shared Models/)
- [ ] Copyright header has correct file name
- [ ] No dependency rule violations (no upward dependencies)
- [ ] `dotnet build` succeeds with 0 warnings
- [ ] `dotnet test` passes all tests
- [ ] Component is properly documented (XML comments)

## Architecture Benefits

This Atomic Design approach provides:

1. **Consistency**: All components follow the same patterns and naming
2. **Reusability**: Lower-level components can be used anywhere
3. **Maintainability**: Clear organization makes components easy to find
4. **Scalability**: Adding new features is straightforward and predictable
5. **Testability**: Each level can be tested independently
6. **Team Collaboration**: Clear rules reduce conflicts and confusion

## References

- [Atomic Design Methodology](https://atomicdesign.bradfrost.com/)
- [Blazor Component Guidelines](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/)
- [TradingBot Component Specification](../../../../specs/004-component-refactor/spec.md)
