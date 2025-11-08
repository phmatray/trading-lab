---
description: "Component Refactoring and Organization Task List"
---

# Tasks: Component Refactoring and Organization

**Input**: Design documents from `/specs/004-component-refactor/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/component-migration-map.md, contracts/namespace-mapping.md, research.md, quickstart.md

**Tests**: NOT INCLUDED - This is a pure refactoring effort. Existing tests must pass without modification to verify functional parity.

**Organization**: Tasks are grouped by user story to enable independent verification of each organizational improvement.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Project root**: `/Users/phmatray/Repositories/github-phm/TradingBot/`
- **Web project**: `src/TradingBot.Web/`
- **Components**: `src/TradingBot.Web/Components/`
- **Pages**: `src/TradingBot.Web/Pages/`

---

## Phase 1: Setup (Verification & Backup)

**Purpose**: Verify baseline and prepare for refactoring

- [X] T001 Verify all existing tests pass with dotnet test
- [X] T002 Create git commit checkpoint before refactoring begins
- [X] T003 Verify dotnet build succeeds with zero errors

**Checkpoint**: Baseline established - refactoring can begin

---

## Phase 2: Foundational (Core Component Infrastructure)

**Purpose**: Migrate core atoms that all other components depend on - MUST complete before any user story work

**⚠️ CRITICAL**: All molecules, organisms, and features depend on these atoms being migrated first

- [X] T004 Create subfolder src/TradingBot.Web/Components/Atoms/TbButton/
- [X] T005 Rename src/TradingBot.Web/Components/Atoms/Button.razor to src/TradingBot.Web/Components/Atoms/TbButton/TbButton.razor
- [X] T006 Move src/TradingBot.Web/Models/ButtonVariant.cs to src/TradingBot.Web/Components/Atoms/TbButton/ButtonVariant.cs
- [X] T007 Update namespace in TbButton.razor to TradingBot.Web.Components.Atoms
- [X] T008 Update namespace in ButtonVariant.cs to TradingBot.Web.Components.Atoms
- [X] T009 Update copyright headers in TbButton.razor and ButtonVariant.cs
- [X] T010 Delete duplicate src/TradingBot.Web/Components/Shared/Button.razor
- [X] T011 [P] Create subfolder src/TradingBot.Web/Components/Atoms/TbIcon/
- [X] T012 [P] Rename src/TradingBot.Web/Components/Atoms/Icon.razor to src/TradingBot.Web/Components/Atoms/TbIcon/TbIcon.razor
- [X] T013 [P] Move src/TradingBot.Web/Models/IconName.cs to src/TradingBot.Web/Components/Atoms/TbIcon/IconName.cs
- [X] T014 [P] Move src/TradingBot.Web/Models/IconVariant.cs to src/TradingBot.Web/Components/Atoms/TbIcon/IconVariant.cs
- [X] T015 [P] Update namespace in TbIcon.razor and supporting types to TradingBot.Web.Components.Atoms
- [X] T016 [P] Update copyright headers in TbIcon files
- [X] T017 [P] Rename src/TradingBot.Web/Components/Atoms/Input.razor to src/TradingBot.Web/Components/Atoms/TbInput.razor
- [X] T018 [P] Update namespace and copyright header in TbInput.razor
- [X] T019 [P] Create subfolder src/TradingBot.Web/Components/Atoms/TbBadge/
- [X] T020 [P] Rename src/TradingBot.Web/Components/Atoms/Badge.razor to src/TradingBot.Web/Components/Atoms/TbBadge/TbBadge.razor
- [X] T021 [P] Split Models/BadgeEnums.cs into TbBadge/BadgeVariant.cs and TbBadge/BadgeSize.cs
- [X] T022 [P] Update namespace and copyright headers in TbBadge files
- [X] T023 [P] Create subfolder src/TradingBot.Web/Components/Atoms/TbLabel/
- [X] T024 [P] Rename src/TradingBot.Web/Components/Atoms/Label.razor to src/TradingBot.Web/Components/Atoms/TbLabel/TbLabel.razor
- [X] T025 [P] Move src/TradingBot.Web/Models/LabelSize.cs to src/TradingBot.Web/Components/Atoms/TbLabel/LabelSize.cs
- [X] T026 [P] Update namespace and copyright headers in TbLabel files
- [X] T027 [P] Rename src/TradingBot.Web/Components/Atoms/Select.razor to src/TradingBot.Web/Components/Atoms/TbSelect.razor
- [X] T028 [P] Update namespace and copyright header in TbSelect.razor
- [X] T029 [P] Create subfolder src/TradingBot.Web/Components/Atoms/TbSpinner/
- [X] T030 [P] Rename src/TradingBot.Web/Components/Atoms/Spinner.razor to src/TradingBot.Web/Components/Atoms/TbSpinner/TbSpinner.razor
- [X] T031 [P] Extract SpinnerSize from Models/SpinnerEnums.cs to TbSpinner/SpinnerSize.cs
- [X] T032 [P] Update namespace and copyright headers in TbSpinner files
- [X] T033 [P] Rename src/TradingBot.Web/Components/Atoms/Toggle.razor to src/TradingBot.Web/Components/Atoms/TbToggle.razor
- [X] T034 [P] Update namespace and copyright header in TbToggle.razor
- [X] T035 Verify dotnet build succeeds after atom migrations
- [X] T036 Commit checkpoint for atom migrations

**Checkpoint**: Foundation ready - atoms complete, user story work can now begin

---

## Phase 3: User Story 1 - Component Consistency Across Application (Priority: P1) 🎯 MVP

**Goal**: Establish consistent, predictable components by adding Tb prefix and eliminating duplicates so all pages use the same component implementations

**Independent Test**: Verify all pages compile successfully and use TbButton instead of Button, verify no duplicate Button components exist in codebase

### Implementation for User Story 1

- [ ] T037 [P] [US1] Find/replace all <Button references with <TbButton in src/TradingBot.Web/Pages/*.razor
- [ ] T038 [P] [US1] Find/replace all <Icon references with <TbIcon in src/TradingBot.Web/Pages/*.razor
- [ ] T039 [P] [US1] Find/replace all <Input references with <TbInput in src/TradingBot.Web/Pages/*.razor
- [ ] T040 [P] [US1] Find/replace all <Badge references with <TbBadge in src/TradingBot.Web/Pages/*.razor
- [ ] T041 [P] [US1] Find/replace all <Label references with <TbLabel in src/TradingBot.Web/Pages/*.razor
- [ ] T042 [P] [US1] Find/replace all <Select references with <TbSelect in src/TradingBot.Web/Pages/*.razor
- [ ] T043 [P] [US1] Find/replace all <Spinner references with <TbSpinner in src/TradingBot.Web/Pages/*.razor
- [ ] T044 [P] [US1] Find/replace all <Toggle references with <TbToggle in src/TradingBot.Web/Pages/*.razor
- [ ] T045 [US1] Verify dotnet build succeeds after all atom reference updates
- [ ] T046 [US1] Verify all existing tests pass unchanged (dotnet test)
- [ ] T047 [US1] Manual browser test: verify all pages render correctly with Tb-prefixed atoms
- [ ] T048 [US1] Commit checkpoint for User Story 1 completion

**Checkpoint**: User Story 1 complete - All pages now use consistent Tb-prefixed atom components, no duplicates

---

## Phase 4: User Story 2 - Improved Developer Navigation and Discoverability (Priority: P2)

**Goal**: Organize components into clear Atomic Design hierarchy (Molecules → Organisms → Features) so developers can quickly locate components

**Independent Test**: Verify all molecule components in Components/Molecules/ folder, all organisms in Components/Organisms/, all feature components in Components/Features/{Domain}/ with no legacy folders remaining

### Implementation for User Story 2 - Molecules

- [ ] T049 [P] [US2] Migrate src/TradingBot.Web/Components/Shared/Card.razor to src/TradingBot.Web/Components/Molecules/TbCard.razor
- [ ] T050 [P] [US2] Update namespace to TradingBot.Web.Components.Molecules and copyright header in TbCard.razor
- [ ] T051 [P] [US2] Update internal component references in TbCard.razor to use TbButton and TbIcon
- [ ] T052 [P] [US2] Migrate src/TradingBot.Web/Components/Shared/Modal.razor to src/TradingBot.Web/Components/Molecules/TbModal.razor
- [ ] T053 [P] [US2] Update namespace and copyright header in TbModal.razor
- [ ] T054 [P] [US2] Update internal component references in TbModal.razor to use TbButton and TbIcon
- [ ] T055 [P] [US2] Migrate src/TradingBot.Web/Components/Shared/Table.razor to src/TradingBot.Web/Components/Molecules/TbTable.razor
- [ ] T056 [P] [US2] Update namespace and copyright header in TbTable.razor
- [ ] T057 [P] [US2] Update internal component references in TbTable.razor to use TbIcon and TbBadge
- [ ] T058 [P] [US2] Rename src/TradingBot.Web/Components/Molecules/FormField.razor to src/TradingBot.Web/Components/Molecules/TbFormField.razor
- [ ] T059 [P] [US2] Update namespace and copyright header in TbFormField.razor
- [ ] T060 [P] [US2] Update internal component references in TbFormField.razor to use TbLabel and TbInput
- [ ] T061 [P] [US2] Rename src/TradingBot.Web/Components/Molecules/MenuItem.razor to src/TradingBot.Web/Components/Molecules/TbMenuItem.razor
- [ ] T062 [P] [US2] Update namespace and copyright header in TbMenuItem.razor
- [ ] T063 [P] [US2] Update internal component references in TbMenuItem.razor to use TbIcon and TbBadge
- [ ] T064 [P] [US2] Create subfolder src/TradingBot.Web/Components/Molecules/TbToast/
- [ ] T065 [P] [US2] Rename src/TradingBot.Web/Components/Molecules/Toast.razor to src/TradingBot.Web/Components/Molecules/TbToast/TbToast.razor
- [ ] T066 [P] [US2] Move src/TradingBot.Web/Models/ToastType.cs to src/TradingBot.Web/Components/Molecules/TbToast/ToastType.cs
- [ ] T067 [P] [US2] Update namespace and copyright headers in TbToast files
- [ ] T068 [P] [US2] Update internal component references in TbToast.razor to use TbIcon and TbButton
- [ ] T069 [P] [US2] Rename src/TradingBot.Web/Components/Molecules/PageHeader.razor to src/TradingBot.Web/Components/Molecules/TbPageHeader.razor
- [ ] T070 [P] [US2] Update namespace and copyright header in TbPageHeader.razor
- [ ] T071 [P] [US2] Update internal component references in TbPageHeader.razor to use TbButton and TbIcon
- [ ] T072 [P] [US2] Create subfolder src/TradingBot.Web/Components/Molecules/TbInfoTooltip/
- [ ] T073 [P] [US2] Rename src/TradingBot.Web/Components/Molecules/InfoTooltip.razor to src/TradingBot.Web/Components/Molecules/TbInfoTooltip/TbInfoTooltip.razor
- [ ] T074 [P] [US2] Move src/TradingBot.Web/Models/TooltipPosition.cs to src/TradingBot.Web/Components/Molecules/TbInfoTooltip/TooltipPosition.cs
- [ ] T075 [P] [US2] Update namespace and copyright headers in TbInfoTooltip files
- [ ] T076 [P] [US2] Rename src/TradingBot.Web/Components/Molecules/TablePagination.razor to src/TradingBot.Web/Components/Molecules/TbTablePagination.razor
- [ ] T077 [P] [US2] Update namespace and copyright header in TbTablePagination.razor
- [ ] T078 [P] [US2] Update internal component references in TbTablePagination.razor to use TbButton

### Implementation for User Story 2 - Organisms

- [ ] T079 [P] [US2] Rename src/TradingBot.Web/Components/Organisms/NavigationSidebar.razor to src/TradingBot.Web/Components/Organisms/TbNavigationSidebar.razor
- [ ] T080 [P] [US2] Merge functionality from src/TradingBot.Web/Components/Layout/NavMenu.razor into TbNavigationSidebar.razor
- [ ] T081 [P] [US2] Delete src/TradingBot.Web/Components/Layout/NavMenu.razor
- [ ] T082 [P] [US2] Update namespace and copyright header in TbNavigationSidebar.razor
- [ ] T083 [P] [US2] Update internal component references in TbNavigationSidebar.razor to use TbMenuItem, TbIcon, TbBadge
- [ ] T084 [P] [US2] Update src/TradingBot.Web/Components/Layout/MainLayout.razor to use TbNavigationSidebar
- [ ] T085 [P] [US2] Migrate src/TradingBot.Web/Components/Shared/ToastContainer.razor to src/TradingBot.Web/Components/Organisms/TbToastContainer.razor
- [ ] T086 [P] [US2] Update namespace and copyright header in TbToastContainer.razor
- [ ] T087 [P] [US2] Update internal component references in TbToastContainer.razor to use TbToast
- [ ] T088 [P] [US2] Migrate src/TradingBot.Web/Components/Shared/ErrorBoundary.razor to src/TradingBot.Web/Components/Organisms/TbErrorBoundary.razor
- [ ] T089 [P] [US2] Update namespace and copyright header in TbErrorBoundary.razor
- [ ] T090 [P] [US2] Update internal component references in TbErrorBoundary.razor to use TbCard, TbButton, TbIcon
- [ ] T091 [P] [US2] Rename src/TradingBot.Web/Components/Organisms/NotificationCenter.razor to src/TradingBot.Web/Components/Organisms/TbNotificationCenter.razor
- [ ] T092 [P] [US2] Update namespace and copyright header in TbNotificationCenter.razor
- [ ] T093 [P] [US2] Update internal component references in TbNotificationCenter.razor to use TbBadge, TbIcon, TbButton
- [ ] T094 [P] [US2] Rename src/TradingBot.Web/Components/Organisms/SettingsForm.razor to src/TradingBot.Web/Components/Organisms/TbSettingsForm.razor
- [ ] T095 [P] [US2] Update namespace and copyright header in TbSettingsForm.razor
- [ ] T096 [P] [US2] Update internal component references in TbSettingsForm.razor to use TbFormField, TbInput, TbSelect, TbToggle, TbButton, TbCard
- [ ] T097 [P] [US2] Rename src/TradingBot.Web/Components/Organisms/ThemeProvider.razor to src/TradingBot.Web/Components/Organisms/TbThemeProvider.razor
- [ ] T098 [P] [US2] Update namespace and copyright header in TbThemeProvider.razor

### Implementation for User Story 2 - Feature Components

- [ ] T099 [US2] Create directory src/TradingBot.Web/Components/Features/Dashboard/
- [ ] T100 [P] [US2] Rename src/TradingBot.Web/Components/Dashboard/DashboardHeader.razor to src/TradingBot.Web/Components/Features/Dashboard/TbDashboardHeader.razor
- [ ] T101 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Dashboard and copyright header in TbDashboardHeader.razor
- [ ] T102 [P] [US2] Update internal component references in TbDashboardHeader.razor to use Tb-prefixed components
- [ ] T103 [P] [US2] Rename src/TradingBot.Web/Components/Dashboard/AccountSummaryCard.razor to src/TradingBot.Web/Components/Features/Dashboard/TbAccountSummaryCard.razor
- [ ] T104 [P] [US2] Update namespace and copyright header in TbAccountSummaryCard.razor
- [ ] T105 [P] [US2] Update internal component references in TbAccountSummaryCard.razor
- [ ] T106 [P] [US2] Rename src/TradingBot.Web/Components/Dashboard/PerformanceMetricsCard.razor to src/TradingBot.Web/Components/Features/Dashboard/TbPerformanceMetricsCard.razor
- [ ] T107 [P] [US2] Update namespace and copyright header in TbPerformanceMetricsCard.razor
- [ ] T108 [P] [US2] Update internal component references in TbPerformanceMetricsCard.razor
- [ ] T109 [P] [US2] Rename src/TradingBot.Web/Components/Dashboard/ActiveStrategiesCard.razor to src/TradingBot.Web/Components/Features/Dashboard/TbActiveStrategiesCard.razor
- [ ] T110 [P] [US2] Update namespace and copyright header in TbActiveStrategiesCard.razor
- [ ] T111 [P] [US2] Update internal component references in TbActiveStrategiesCard.razor
- [ ] T112 [P] [US2] Rename src/TradingBot.Web/Components/Dashboard/RecentTradesCard.razor to src/TradingBot.Web/Components/Features/Dashboard/TbRecentTradesCard.razor
- [ ] T113 [P] [US2] Update namespace and copyright header in TbRecentTradesCard.razor
- [ ] T114 [P] [US2] Update internal component references in TbRecentTradesCard.razor
- [ ] T115 [P] [US2] Rename src/TradingBot.Web/Components/Dashboard/MarketOverviewCard.razor to src/TradingBot.Web/Components/Features/Dashboard/TbMarketOverviewCard.razor
- [ ] T116 [P] [US2] Update namespace and copyright header in TbMarketOverviewCard.razor
- [ ] T117 [P] [US2] Update internal component references in TbMarketOverviewCard.razor
- [ ] T118 [US2] Create directory src/TradingBot.Web/Components/Features/Portfolio/
- [ ] T119 [P] [US2] Rename src/TradingBot.Web/Components/Portfolio/PortfolioSummary.razor to src/TradingBot.Web/Components/Features/Portfolio/TbPortfolioSummary.razor
- [ ] T120 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Portfolio and copyright header in TbPortfolioSummary.razor
- [ ] T121 [P] [US2] Update internal component references in TbPortfolioSummary.razor
- [ ] T122 [P] [US2] Rename src/TradingBot.Web/Components/Portfolio/PositionCard.razor to src/TradingBot.Web/Components/Features/Portfolio/TbPositionCard.razor
- [ ] T123 [P] [US2] Update namespace and copyright header in TbPositionCard.razor
- [ ] T124 [P] [US2] Update internal component references in TbPositionCard.razor
- [ ] T125 [P] [US2] Rename src/TradingBot.Web/Components/Portfolio/PortfolioChart.razor to src/TradingBot.Web/Components/Features/Portfolio/TbPortfolioChart.razor
- [ ] T126 [P] [US2] Update namespace and copyright header in TbPortfolioChart.razor
- [ ] T127 [P] [US2] Update internal component references in TbPortfolioChart.razor
- [ ] T128 [P] [US2] Rename src/TradingBot.Web/Components/Portfolio/AssetAllocationChart.razor to src/TradingBot.Web/Components/Features/Portfolio/TbAssetAllocationChart.razor
- [ ] T129 [P] [US2] Update namespace and copyright header in TbAssetAllocationChart.razor
- [ ] T130 [P] [US2] Update internal component references in TbAssetAllocationChart.razor
- [ ] T131 [US2] Create directory src/TradingBot.Web/Components/Features/Strategy/
- [ ] T132 [P] [US2] Rename src/TradingBot.Web/Components/Strategy/StrategyCard.razor to src/TradingBot.Web/Components/Features/Strategy/TbStrategyCard.razor
- [ ] T133 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Strategy and copyright header in TbStrategyCard.razor
- [ ] T134 [P] [US2] Update internal component references in TbStrategyCard.razor
- [ ] T135 [P] [US2] Rename src/TradingBot.Web/Components/Strategy/StrategyConfigForm.razor to src/TradingBot.Web/Components/Features/Strategy/TbStrategyConfigForm.razor
- [ ] T136 [P] [US2] Update namespace and copyright header in TbStrategyConfigForm.razor
- [ ] T137 [P] [US2] Update internal component references in TbStrategyConfigForm.razor
- [ ] T138 [US2] Create directory src/TradingBot.Web/Components/Features/Risk/
- [ ] T139 [P] [US2] Rename src/TradingBot.Web/Components/Risk/RiskMetricsCard.razor to src/TradingBot.Web/Components/Features/Risk/TbRiskMetricsCard.razor
- [ ] T140 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Risk and copyright header in TbRiskMetricsCard.razor
- [ ] T141 [P] [US2] Update internal component references in TbRiskMetricsCard.razor
- [ ] T142 [P] [US2] Rename src/TradingBot.Web/Components/Risk/RiskLimitsForm.razor to src/TradingBot.Web/Components/Features/Risk/TbRiskLimitsForm.razor
- [ ] T143 [P] [US2] Update namespace and copyright header in TbRiskLimitsForm.razor
- [ ] T144 [P] [US2] Update internal component references in TbRiskLimitsForm.razor
- [ ] T145 [US2] Create directory src/TradingBot.Web/Components/Features/Performance/
- [ ] T146 [P] [US2] Rename src/TradingBot.Web/Components/Performance/EquityCurveChart.razor to src/TradingBot.Web/Components/Features/Performance/TbEquityCurveChart.razor
- [ ] T147 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Performance and copyright header in TbEquityCurveChart.razor
- [ ] T148 [P] [US2] Update internal component references in TbEquityCurveChart.razor
- [ ] T149 [P] [US2] Rename src/TradingBot.Web/Components/Performance/PerformanceStatsCard.razor to src/TradingBot.Web/Components/Features/Performance/TbPerformanceStatsCard.razor
- [ ] T150 [P] [US2] Update namespace and copyright header in TbPerformanceStatsCard.razor
- [ ] T151 [P] [US2] Update internal component references in TbPerformanceStatsCard.razor
- [ ] T152 [US2] Create directory src/TradingBot.Web/Components/Features/Backtest/
- [ ] T153 [P] [US2] Rename src/TradingBot.Web/Components/Backtest/BacktestConfigForm.razor to src/TradingBot.Web/Components/Features/Backtest/TbBacktestConfigForm.razor
- [ ] T154 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Backtest and copyright header in TbBacktestConfigForm.razor
- [ ] T155 [P] [US2] Update internal component references in TbBacktestConfigForm.razor
- [ ] T156 [P] [US2] Rename src/TradingBot.Web/Components/Backtest/BacktestResultsCard.razor to src/TradingBot.Web/Components/Features/Backtest/TbBacktestResultsCard.razor
- [ ] T157 [P] [US2] Update namespace and copyright header in TbBacktestResultsCard.razor
- [ ] T158 [P] [US2] Update internal component references in TbBacktestResultsCard.razor
- [ ] T159 [P] [US2] Rename src/TradingBot.Web/Components/Backtest/BacktestChart.razor to src/TradingBot.Web/Components/Features/Backtest/TbBacktestChart.razor
- [ ] T160 [P] [US2] Update namespace and copyright header in TbBacktestChart.razor
- [ ] T161 [P] [US2] Update internal component references in TbBacktestChart.razor
- [ ] T162 [US2] Create directory src/TradingBot.Web/Components/Features/Charts/
- [ ] T163 [P] [US2] Rename src/TradingBot.Web/Components/Charts/CandlestickChart.razor to src/TradingBot.Web/Components/Features/Charts/TbCandlestickChart.razor
- [ ] T164 [P] [US2] Update namespace to TradingBot.Web.Components.Features.Charts and copyright header in TbCandlestickChart.razor
- [ ] T165 [P] [US2] Update internal component references in TbCandlestickChart.razor
- [ ] T166 [P] [US2] Rename src/TradingBot.Web/Components/Charts/LineChart.razor to src/TradingBot.Web/Components/Features/Charts/TbLineChart.razor
- [ ] T167 [P] [US2] Update namespace and copyright header in TbLineChart.razor
- [ ] T168 [P] [US2] Update internal component references in TbLineChart.razor

### Verification for User Story 2

- [ ] T169 [US2] Update all molecule/organism/feature component references in src/TradingBot.Web/Pages/*.razor to use Tb-prefixed names
- [ ] T170 [US2] Update all component references in src/TradingBot.Web/Components/Layout/MainLayout.razor
- [ ] T171 [US2] Verify dotnet build succeeds after all component migrations
- [ ] T172 [US2] Verify all existing tests pass unchanged (dotnet test)
- [ ] T173 [US2] Manual browser test: verify all pages render correctly with Tb-prefixed components
- [ ] T174 [US2] Verify no components remain in src/TradingBot.Web/Components/Shared/ folder
- [ ] T175 [US2] Verify all feature components in correct Features/{Domain}/ subfolders
- [ ] T176 [US2] Commit checkpoint for User Story 2 completion

**Checkpoint**: User Story 2 complete - All components organized in clear Atomic Design hierarchy with feature-based organization

---

## Phase 5: User Story 3 - Reduced Code Duplication and Maintenance Burden (Priority: P2)

**Goal**: Consolidate duplicate components and co-locate supporting types to establish single source of truth for each component

**Independent Test**: Verify no duplicate component implementations exist, verify all supporting enums co-located with their components, verify single _Imports.razor file

### Implementation for User Story 3 - Pages Consolidation

- [ ] T177 [P] [US3] Move src/TradingBot.Web/Components/Pages/Settings.razor to src/TradingBot.Web/Pages/Settings.razor
- [ ] T178 [P] [US3] Update all component references in Pages/Settings.razor to use Tb-prefixed components
- [ ] T179 [P] [US3] Move src/TradingBot.Web/Components/Pages/Help.razor to src/TradingBot.Web/Pages/Help.razor
- [ ] T180 [P] [US3] Update all component references in Pages/Help.razor to use Tb-prefixed components
- [ ] T181 [US3] Update all component references in src/TradingBot.Web/Pages/Home.razor to use Tb-prefixed components
- [ ] T182 [US3] Update all component references in src/TradingBot.Web/Pages/Dashboard.razor to use Tb-prefixed Dashboard feature components
- [ ] T183 [US3] Update all component references in src/TradingBot.Web/Pages/Portfolio.razor to use Tb-prefixed Portfolio feature components
- [ ] T184 [US3] Update all component references in src/TradingBot.Web/Pages/Strategies.razor to use Tb-prefixed Strategy feature components
- [ ] T185 [US3] Update all component references in src/TradingBot.Web/Pages/Backtest.razor to use Tb-prefixed Backtest feature components

### Implementation for User Story 3 - Import Consolidation

- [ ] T186 [US3] Create consolidated src/TradingBot.Web/Components/_Imports.razor with all necessary namespaces
- [ ] T187 [US3] Add @using TradingBot.Web.Components.Atoms to _Imports.razor
- [ ] T188 [US3] Add @using TradingBot.Web.Components.Molecules to _Imports.razor
- [ ] T189 [US3] Add @using TradingBot.Web.Components.Organisms to _Imports.razor
- [ ] T190 [US3] Add @using TradingBot.Web.Components.Features.Dashboard to _Imports.razor
- [ ] T191 [US3] Add @using TradingBot.Web.Components.Features.Portfolio to _Imports.razor
- [ ] T192 [US3] Add @using TradingBot.Web.Components.Features.Strategy to _Imports.razor
- [ ] T193 [US3] Add @using TradingBot.Web.Components.Features.Risk to _Imports.razor
- [ ] T194 [US3] Add @using TradingBot.Web.Components.Features.Performance to _Imports.razor
- [ ] T195 [US3] Add @using TradingBot.Web.Components.Features.Backtest to _Imports.razor
- [ ] T196 [US3] Add @using TradingBot.Web.Components.Features.Charts to _Imports.razor
- [ ] T197 [US3] Delete src/TradingBot.Web/Pages/_Imports.razor (now inherited from Components/_Imports.razor)
- [ ] T198 [US3] Verify dotnet build succeeds with consolidated imports
- [ ] T199 [US3] Use IDE to remove unused using statements from consolidated _Imports.razor

### Verification for User Story 3

- [ ] T200 [US3] Verify only one _Imports.razor file exists in src/TradingBot.Web/Components/
- [ ] T201 [US3] Verify all pages exist in src/TradingBot.Web/Pages/ folder only
- [ ] T202 [US3] Verify all supporting enums co-located with their components (no component enums in Models/)
- [ ] T203 [US3] Search codebase for duplicate component names and verify none exist
- [ ] T204 [US3] Verify dotnet build succeeds
- [ ] T205 [US3] Verify all existing tests pass unchanged (dotnet test)
- [ ] T206 [US3] Manual browser test: verify all pages render correctly
- [ ] T207 [US3] Commit checkpoint for User Story 3 completion

**Checkpoint**: User Story 3 complete - Single source of truth established, duplicates eliminated, imports consolidated

---

## Phase 6: User Story 4 - Clear Atomic Design Hierarchy (Priority: P3)

**Goal**: Verify and enforce clear Atomic Design dependency rules so developers understand component composition and reuse patterns

**Independent Test**: Verify no atoms depend on molecules, no molecules depend on organisms, verify all components follow classification rules from data-model.md

### Implementation for User Story 4

- [ ] T208 [US4] Create documentation comment in src/TradingBot.Web/Components/Atoms/README.md explaining atom definition and dependency rules
- [ ] T209 [US4] Create documentation comment in src/TradingBot.Web/Components/Molecules/README.md explaining molecule definition and dependency rules
- [ ] T210 [US4] Create documentation comment in src/TradingBot.Web/Components/Organisms/README.md explaining organism definition and dependency rules
- [ ] T211 [US4] Create documentation comment in src/TradingBot.Web/Components/Features/README.md explaining feature component organization
- [ ] T212 [US4] Verify TbButton.razor has no dependencies on other Tb components (only HTML elements)
- [ ] T213 [US4] Verify TbInput.razor has no dependencies on other Tb components
- [ ] T214 [US4] Verify TbIcon.razor has no dependencies on other Tb components
- [ ] T215 [US4] Verify TbBadge.razor has no dependencies on other Tb components
- [ ] T216 [US4] Verify TbLabel.razor has no dependencies on other Tb components
- [ ] T217 [US4] Verify TbSelect.razor has no dependencies on other Tb components
- [ ] T218 [US4] Verify TbSpinner.razor has no dependencies on other Tb components
- [ ] T219 [US4] Verify TbToggle.razor has no dependencies on other Tb components
- [ ] T220 [US4] Verify TbCard.razor only depends on atoms (TbButton, TbIcon), not molecules or organisms
- [ ] T221 [US4] Verify TbModal.razor only depends on atoms
- [ ] T222 [US4] Verify TbTable.razor only depends on atoms
- [ ] T223 [US4] Verify TbFormField.razor only depends on atoms
- [ ] T224 [US4] Verify TbMenuItem.razor only depends on atoms
- [ ] T225 [US4] Verify TbToast.razor only depends on atoms
- [ ] T226 [US4] Verify TbPageHeader.razor only depends on atoms
- [ ] T227 [US4] Verify TbInfoTooltip.razor only depends on atoms
- [ ] T228 [US4] Verify TbTablePagination.razor only depends on atoms
- [ ] T229 [US4] Verify TbNavigationSidebar.razor depends only on atoms and molecules (TbMenuItem)
- [ ] T230 [US4] Verify TbToastContainer.razor depends only on molecules (TbToast)
- [ ] T231 [US4] Verify TbErrorBoundary.razor depends only on atoms and molecules
- [ ] T232 [US4] Verify TbNotificationCenter.razor depends only on atoms and molecules
- [ ] T233 [US4] Verify TbSettingsForm.razor depends only on atoms and molecules
- [ ] T234 [US4] Verify all Dashboard feature components appropriately depend on atoms/molecules/organisms
- [ ] T235 [US4] Verify all Portfolio feature components appropriately depend on atoms/molecules/organisms
- [ ] T236 [US4] Verify all Strategy feature components appropriately depend on atoms/molecules/organisms
- [ ] T237 [US4] Verify all Risk feature components appropriately depend on atoms/molecules/organisms
- [ ] T238 [US4] Verify all Performance feature components appropriately depend on atoms/molecules/organisms
- [ ] T239 [US4] Verify all Backtest feature components appropriately depend on atoms/molecules/organisms
- [ ] T240 [US4] Verify all Charts feature components appropriately depend on atoms/molecules/organisms

### Verification for User Story 4

- [ ] T241 [US4] Verify no circular dependencies exist between components
- [ ] T242 [US4] Verify component hierarchy documented in README files
- [ ] T243 [US4] Verify dotnet build succeeds
- [ ] T244 [US4] Verify all existing tests pass unchanged (dotnet test)
- [ ] T245 [US4] Commit checkpoint for User Story 4 completion

**Checkpoint**: User Story 4 complete - Atomic Design hierarchy verified and documented with clear dependency rules

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup and verification across all user stories

- [ ] T246 [P] Delete empty src/TradingBot.Web/Components/Shared/ folder
- [ ] T247 [P] Delete empty src/TradingBot.Web/Components/Pages/ folder
- [ ] T248 [P] Delete empty src/TradingBot.Web/Components/Dashboard/ folder
- [ ] T249 [P] Delete empty src/TradingBot.Web/Components/Portfolio/ folder
- [ ] T250 [P] Delete empty src/TradingBot.Web/Components/Strategy/ folder
- [ ] T251 [P] Delete empty src/TradingBot.Web/Components/Risk/ folder
- [ ] T252 [P] Delete empty src/TradingBot.Web/Components/Performance/ folder
- [ ] T253 [P] Delete empty src/TradingBot.Web/Components/Backtest/ folder
- [ ] T254 [P] Delete empty src/TradingBot.Web/Components/Charts/ folder
- [ ] T255 Verify dotnet build /p:RunAnalyzers=true shows zero warnings (StyleCop compliance)
- [ ] T256 Verify dotnet build succeeds with zero errors
- [ ] T257 Verify dotnet test passes all tests without modification
- [ ] T258 Manual browser test: Home page renders correctly
- [ ] T259 Manual browser test: Dashboard page renders correctly
- [ ] T260 Manual browser test: Portfolio page renders correctly
- [ ] T261 Manual browser test: Strategies page renders correctly
- [ ] T262 Manual browser test: Backtest page renders correctly
- [ ] T263 Manual browser test: Settings page renders correctly
- [ ] T264 Manual browser test: Help page renders correctly
- [ ] T265 Verify all copyright headers have correct file names
- [ ] T266 Verify all namespaces follow hierarchical structure per namespace-mapping.md
- [ ] T267 Run quickstart.md validation scenarios
- [ ] T268 Create final git commit for complete refactoring

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories (atoms must be migrated before molecules/organisms/features can use them)
- **User Story 1 (Phase 3)**: Depends on Foundational phase - Can proceed independently
- **User Story 2 (Phase 4)**: Depends on Foundational phase - Can proceed independently (or after US1 for safety)
- **User Story 3 (Phase 5)**: Depends on US1 and US2 completion (needs all components migrated before consolidation)
- **User Story 4 (Phase 6)**: Depends on US1, US2, US3 completion (verification of final structure)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational (atoms migrated) - MVP candidate
- **User Story 2 (P2)**: Depends on Foundational - Adds molecule/organism/feature organization
- **User Story 3 (P2)**: Depends on US1 and US2 - Consolidates duplicates and imports
- **User Story 4 (P3)**: Depends on US1, US2, US3 - Verifies and documents final hierarchy

### Parallel Opportunities

- Within Foundational phase: Many atom migrations marked [P] can run in parallel (different component subfolders)
- Within User Story 1: All page updates marked [P] can run in parallel (different page files)
- Within User Story 2: Many molecule/organism/feature migrations marked [P] can run in parallel (different component files)
- Within User Story 3: Page consolidation tasks marked [P] can run in parallel
- Polish phase: Folder deletions marked [P] can run in parallel

---

## Parallel Example: Foundational Phase

```bash
# Launch multiple atom migrations together (different subfolders):
Task: "Create subfolder src/TradingBot.Web/Components/Atoms/TbIcon/"
Task: "Create subfolder src/TradingBot.Web/Components/Atoms/TbBadge/"
Task: "Create subfolder src/TradingBot.Web/Components/Atoms/TbLabel/"

# Rename multiple atoms in parallel:
Task: "Rename Input.razor to TbInput.razor"
Task: "Rename Select.razor to TbSelect.razor"
Task: "Rename Toggle.razor to TbToggle.razor"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (atoms migrated)
3. Complete Phase 3: User Story 1 (consistent atom usage across pages)
4. Complete Phase 4: User Story 2 (full Atomic Design organization)
5. **STOP and VALIDATE**: Test all pages, verify organization
6. Deploy/demo refactored structure

### Incremental Delivery

1. Setup + Foundational → Atoms ready
2. Add User Story 1 → Test independently → All pages use consistent Tb atoms
3. Add User Story 2 → Test independently → Full Atomic Design hierarchy in place
4. Add User Story 3 → Test independently → Duplicates eliminated, imports consolidated
5. Add User Story 4 → Test independently → Hierarchy verified and documented
6. Polish → Final cleanup and verification

### Sequential Strategy (Recommended for Safety)

1. Complete Setup + Foundational
2. Complete User Story 1 → Verify
3. Complete User Story 2 → Verify
4. Complete User Story 3 → Verify
5. Complete User Story 4 → Verify
6. Polish → Verify

---

## Notes

- [P] tasks = different files/folders, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- This is a PURE REFACTORING effort - zero functionality changes allowed
- All existing tests must pass without modification at every checkpoint
- Commit at each checkpoint for rollback capability
- Manual browser testing required at each checkpoint to verify rendering
- StyleCop compliance required throughout (copyright headers, namespaces)
- Supporting types co-located with components (enums in component subfolders)
- Single _Imports.razor at Components root eliminates duplication