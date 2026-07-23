# Tasks: Component Refactoring and Organization

**Feature**: Component Refactoring and Organization (004-component-refactor)
**Input**: Design documents from `/specs/004-component-refactor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/component-migration-map.md

**Tests**: No test generation requested in specification - existing tests must pass unchanged

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project structure preparation for refactoring

- [X] T001 Verify TradingBot.Web project builds successfully before refactoring
- [X] T002 Create backup commit point for rollback capability
- [X] T003 Create Features folder structure in src/TradingBot.Web/Components/Features/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 [P] Migrate TbButton: Create src/TradingBot.Web/Components/Atoms/TbButton/ subfolder, rename Button.razor to TbButton.razor, move Models/ButtonVariant.cs to subfolder, update namespaces and copyright headers
- [X] T005 [P] Migrate TbInput: Rename src/TradingBot.Web/Components/Atoms/Input.razor to TbInput.razor, update namespace and copyright header
- [X] T006 [P] Migrate TbIcon: Create src/TradingBot.Web/Components/Atoms/TbIcon/ subfolder, rename Icon.razor to TbIcon.razor, move Models/IconName.cs and IconVariant.cs to subfolder, update namespaces
- [X] T007 [P] Migrate TbBadge: Create src/TradingBot.Web/Components/Atoms/TbBadge/ subfolder, rename Badge.razor to TbBadge.razor, split Models/BadgeEnums.cs into BadgeVariant.cs and BadgeSize.cs in subfolder
- [X] T008 [P] Migrate TbLabel: Create src/TradingBot.Web/Components/Atoms/TbLabel/ subfolder, rename Label.razor to TbLabel.razor, move Models/LabelSize.cs to subfolder
- [X] T009 [P] Migrate TbSelect: Rename src/TradingBot.Web/Components/Atoms/Select.razor to TbSelect.razor, update namespace and copyright header
- [X] T010 [P] Migrate TbSpinner: Create src/TradingBot.Web/Components/Atoms/TbSpinner/ subfolder, rename Spinner.razor to TbSpinner.razor, extract SpinnerSize from Models/SpinnerEnums.cs to subfolder
- [X] T011 [P] Migrate TbToggle: Rename src/TradingBot.Web/Components/Atoms/Toggle.razor to TbToggle.razor, update namespace and copyright header
- [X] T012 Update all Atom component internal namespaces to TradingBot.Web.Components.Atoms
- [X] T013 Verify atoms build: Run dotnet build and dotnet test to verify all atom migrations successful

**Checkpoint**: Foundation atoms ready - molecule and organism implementation can now begin

---

## Phase 3: User Story 1 - Component Consistency Across Application (Priority: P1) 🎯 MVP

**Goal**: Eliminate duplicate components and establish single source of truth for all UI components with Tb prefix

**Independent Test**: Verify all pages use the same component implementations (e.g., all buttons use TbButton, no usage of duplicate Button components). Search codebase for duplicate component references.

### Implementation for User Story 1

- [X] T014 [P] [US1] Migrate TbCard: Move Components/Shared/Card.razor to Components/Molecules/TbCard.razor, update namespace to TradingBot.Web.Components.Molecules, update internal references to TbButton and TbIcon
- [X] T015 [P] [US1] Migrate TbModal: Move Components/Shared/Modal.razor to Components/Molecules/TbModal.razor, update namespace and internal references to TbButton and TbIcon
- [X] T016 [P] [US1] Migrate TbTable: Move Components/Shared/Table.razor to Components/Molecules/TbTable.razor, update namespace and internal references to TbIcon and TbBadge
- [X] T017 [P] [US1] Migrate TbFormField: Rename Components/Molecules/FormField.razor to TbFormField.razor, update namespace and internal references to TbLabel and TbInput
- [X] T018 [P] [US1] Migrate TbMenuItem: Rename Components/Molecules/MenuItem.razor to TbMenuItem.razor, update namespace and internal references to TbIcon and TbBadge
- [X] T019 [P] [US1] Migrate TbToast: Create Components/Molecules/TbToast/ subfolder, rename Toast.razor to TbToast.razor, move Models/ToastType.cs to subfolder, update namespaces
- [X] T020 [P] [US1] Migrate TbPageHeader: Rename Components/Molecules/PageHeader.razor to TbPageHeader.razor, update namespace and internal references to TbButton and TbIcon
- [X] T021 [P] [US1] Migrate TbInfoTooltip: Create Components/Molecules/TbInfoTooltip/ subfolder, rename InfoTooltip.razor to TbInfoTooltip.razor, move Models/TooltipPosition.cs to subfolder
- [X] T022 [P] [US1] Migrate TbTablePagination: Rename Components/Molecules/TablePagination.razor to TbTablePagination.razor, update namespace and internal references to TbButton
- [X] T023 [US1] Update all Molecule component namespaces to TradingBot.Web.Components.Molecules and verify internal Tb-prefixed references
- [X] T024 [US1] Find and replace all page references: Update <Card to <TbCard, <Modal to <TbModal, <Table to <TbTable, <Button to <TbButton across all .razor files
- [X] T025 [US1] Verify molecule migration: Run dotnet build and dotnet test to confirm all molecules migrated successfully
- [X] T026 [P] [US1] Migrate TbToastContainer: Move Components/Shared/ToastContainer.razor to Components/Organisms/TbToastContainer.razor, update namespace to TradingBot.Web.Components.Organisms, update references to TbToast
- [X] T027 [P] [US1] Migrate TbErrorBoundary: Move Components/Shared/ErrorBoundary.razor to Components/Organisms/TbErrorBoundary.razor, update namespace and references to TbCard, TbButton, TbIcon
- [X] T028 [P] [US1] Migrate TbNotificationCenter: Rename Components/Organisms/NotificationCenter.razor to TbNotificationCenter.razor, update namespace and references to TbBadge, TbIcon, TbButton
- [X] T029 [P] [US1] Migrate TbSettingsForm: Rename Components/Organisms/SettingsForm.razor to TbSettingsForm.razor, update namespace and references to TbFormField, TbInput, TbSelect, TbToggle, TbButton, TbCard
- [X] T030 [P] [US1] Migrate TbThemeProvider: Rename Components/Organisms/ThemeProvider.razor to TbThemeProvider.razor, update namespace
- [X] T031 [US1] Consolidate TbNavigationSidebar: Rename Components/Organisms/NavigationSidebar.razor to TbNavigationSidebar.razor, merge functionality from Components/Layout/NavMenu.razor, delete NavMenu.razor, update MainLayout.razor reference
- [X] T032 [US1] Update all Organism component namespaces to TradingBot.Web.Components.Organisms and verify all internal Tb-prefixed references
- [X] T033 [US1] Delete duplicate Components/Shared/Button.razor (now using TbButton from Atoms)
- [X] T034 [US1] Verify organism migration and duplicate elimination: Run dotnet build and dotnet test to confirm zero duplicate components exist
- [X] T035 [US1] Verify Component Shared folder is empty and delete src/TradingBot.Web/Components/Shared/
- [X] T036 [US1] Final verification: Search codebase for non-Tb component references (grep for "<Button", "<Card", "<Modal" etc.) and confirm zero matches

**Checkpoint**: User Story 1 complete - All components use Tb prefix, duplicates eliminated, single source of truth established

---

## Phase 4: User Story 2 - Improved Developer Navigation and Discoverability (Priority: P2)

**Goal**: Organize all feature-specific components into logical domain folders under Features/ for easy navigation and discoverability

**Independent Test**: Time how long it takes a developer to locate specific components (e.g., "find the position card component") and verify all related files are co-located

### Implementation for User Story 2

- [X] T037 [P] [US2] Create Dashboard feature folder: mkdir -p src/TradingBot.Web/Components/Features/Dashboard
- [X] T038 [P] [US2] Create Portfolio feature folder: mkdir -p src/TradingBot.Web/Components/Features/Portfolio
- [X] T039 [P] [US2] Create Strategy feature folder: mkdir -p src/TradingBot.Web/Components/Features/Strategy
- [X] T040 [P] [US2] Create Risk feature folder: mkdir -p src/TradingBot.Web/Components/Features/Risk
- [X] T041 [P] [US2] Create Performance feature folder: mkdir -p src/TradingBot.Web/Components/Features/Performance
- [X] T042 [P] [US2] Create Backtest feature folder: mkdir -p src/TradingBot.Web/Components/Features/Backtest
- [X] T043 [P] [US2] Create Charts feature folder: mkdir -p src/TradingBot.Web/Components/Features/Charts
- [X] T044 [P] [US2] Migrate Dashboard components: Move and rename DashboardHeader.razor to TbDashboardHeader.razor, AccountSummaryCard.razor to TbAccountSummaryCard.razor, PerformanceMetricsCard.razor to TbPerformanceMetricsCard.razor, ActiveStrategiesCard.razor to TbActiveStrategiesCard.razor, RecentTradesCard.razor to TbRecentTradesCard.razor, MarketOverviewCard.razor to TbMarketOverviewCard.razor into Features/Dashboard/
- [X] T045 [P] [US2] Migrate Portfolio components: Move and rename PortfolioSummary.razor to TbPortfolioSummary.razor, PositionCard.razor to TbPositionCard.razor, PortfolioChart.razor to TbPortfolioChart.razor, AssetAllocationChart.razor to TbAssetAllocationChart.razor into Features/Portfolio/
- [X] T046 [P] [US2] Migrate Strategy components: Move and rename StrategyCard.razor to TbStrategyCard.razor, StrategyConfigForm.razor to TbStrategyConfigForm.razor into Features/Strategy/
- [X] T047 [P] [US2] Migrate Risk components: Move and rename RiskMetricsCard.razor to TbRiskMetricsCard.razor, RiskLimitsForm.razor to TbRiskLimitsForm.razor into Features/Risk/
- [X] T048 [P] [US2] Migrate Performance components: Move and rename EquityCurveChart.razor to TbEquityCurveChart.razor, PerformanceStatsCard.razor to TbPerformanceStatsCard.razor into Features/Performance/
- [X] T049 [P] [US2] Migrate Backtest components: Move and rename BacktestConfigForm.razor to TbBacktestConfigForm.razor, BacktestResultsCard.razor to TbBacktestResultsCard.razor, BacktestChart.razor to TbBacktestChart.razor into Features/Backtest/
- [X] T050 [P] [US2] Migrate Charts components: Move and rename CandlestickChart.razor to TbCandlestickChart.razor, LineChart.razor to TbLineChart.razor into Features/Charts/
- [X] T051 [US2] Update all Dashboard component namespaces to TradingBot.Web.Components.Features.Dashboard, update copyright headers, update internal component references to Tb-prefixed versions
- [X] T052 [US2] Update all Portfolio component namespaces to TradingBot.Web.Components.Features.Portfolio, update copyright headers, update internal component references
- [X] T053 [US2] Update all Strategy component namespaces to TradingBot.Web.Components.Features.Strategy, update copyright headers, update internal component references
- [X] T054 [US2] Update all Risk component namespaces to TradingBot.Web.Components.Features.Risk, update copyright headers, update internal component references
- [X] T055 [US2] Update all Performance component namespaces to TradingBot.Web.Components.Features.Performance, update copyright headers, update internal component references
- [X] T056 [US2] Update all Backtest component namespaces to TradingBot.Web.Components.Features.Backtest, update copyright headers, update internal component references
- [X] T057 [US2] Update all Charts component namespaces to TradingBot.Web.Components.Features.Charts, update copyright headers, update internal component references
- [X] T058 [US2] Update Pages/Dashboard.razor to use new TbDashboard* component names and namespaces
- [X] T059 [US2] Update Pages/Portfolio.razor to use new TbPortfolio* component names and namespaces
- [X] T060 [US2] Update Pages/Strategies.razor to use new TbStrategy* component names and namespaces
- [X] T061 [US2] Update Pages/Backtest.razor to use new TbBacktest* component names and namespaces
- [X] T062 [US2] Delete old feature folders: Remove Components/Dashboard/, Components/Portfolio/, Components/Strategy/, Components/Risk/, Components/Performance/, Components/Backtest/, Components/Charts/
- [X] T063 [US2] Verify feature organization: Run dotnet build and dotnet test to confirm all 21 feature components migrated successfully
- [X] T064 [US2] Move Components/Pages/Settings.razor to Pages/Settings.razor, update component references to Tb-prefixed versions
- [X] T065 [US2] Move Components/Pages/Help.razor to Pages/Help.razor, update component references to Tb-prefixed versions
- [X] T066 [US2] Delete Components/Pages/ folder after verifying it's empty
- [X] T067 [US2] Update Pages/Home.razor to use Tb-prefixed component references
- [X] T068 [US2] Final page verification: Run dotnet build and dotnet test to confirm all pages updated and routable

**Checkpoint**: User Story 2 complete - All 21 feature components organized in Features/ subfolders, all 7 pages consolidated in Pages/ with Tb-prefixed references

---

## Phase 5: User Story 3 - Reduced Code Duplication and Maintenance Burden (Priority: P2)

**Goal**: Consolidate imports into single _Imports.razor file and co-locate supporting types with components

**Independent Test**: Verify single _Imports.razor exists with zero unused imports, search for duplicate component-specific enums in Models/

### Implementation for User Story 3

- [X] T069 [US3] Create consolidated src/TradingBot.Web/Components/_Imports.razor with all necessary namespaces (Blazor core, TradingBot.Core, all component namespaces, Services, Models)
- [X] T070 [US3] Delete src/TradingBot.Web/Pages/_Imports.razor (pages will inherit from Components/_Imports.razor)
- [X] T071 [US3] Verify all pages and components compile with consolidated imports: Run dotnet build
- [X] T072 [US3] Use IDE "Remove Unused Usings" feature on Components/_Imports.razor to eliminate unused imports
- [X] T073 [US3] Verify no unused imports remain: Run dotnet build and confirm zero namespace warnings
- [X] T074 [US3] Final import consolidation verification: Confirm single _Imports.razor at Components root, zero duplicate import files

**Checkpoint**: User Story 3 complete - Single _Imports.razor with zero unused imports, all supporting types co-located with components

---

## Phase 6: User Story 4 - Clear Atomic Design Hierarchy (Priority: P3)

**Goal**: Document and verify component hierarchy follows Atomic Design dependency rules (Atoms → Molecules → Organisms → Features)

**Independent Test**: Review component hierarchy and verify no atoms depend on molecules, no molecules depend on organisms, run build to confirm no circular dependencies

### Implementation for User Story 4

- [X] T075 [US4] Verify Atom dependency rule: Grep all Atom components for references to Molecule/Organism components (should be zero matches)
- [X] T076 [US4] Verify Molecule dependency rule: Grep all Molecule components for references to Organism components (should be zero matches)
- [X] T077 [US4] Verify Organism dependencies: Confirm Organisms only reference Atoms and Molecules (no circular dependencies)
- [X] T078 [US4] Verify Feature dependencies: Confirm Feature components appropriately reference all levels as needed
- [X] T079 [US4] Run final build verification: Execute dotnet build to confirm zero circular dependency errors
- [X] T080 [US4] Create component hierarchy README: Document classification rules (Atoms/Molecules/Organisms/Features) in src/TradingBot.Web/Components/README.md

**Checkpoint**: User Story 4 complete - Component hierarchy verified with zero dependency violations, classification guidelines documented

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and cleanup

- [X] T081 [P] Run StyleCop verification: Execute dotnet build /p:RunAnalyzers=true and confirm zero warnings
- [X] T082 [P] Verify all copyright headers updated with correct file names across all components
- [X] T083 [P] Run full test suite: Execute dotnet test and verify 100% pass rate (existing tests unchanged)
- [ ] T084 Manual browser testing: Test all 7 pages (Home, Dashboard, Portfolio, Strategies, Backtest, Settings, Help) for rendering correctness
- [ ] T085 Verify SignalR real-time updates continue working in Dashboard and Portfolio pages
- [X] T086 Final verification checklist: Confirm all 44 components use Tb prefix, 7 duplicates removed, Features/ organized, single _Imports.razor, zero StyleCop warnings

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories (Atoms must be migrated first as they have no dependencies)
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion (Atoms must exist before Molecules/Organisms can be migrated)
- **User Story 2 (Phase 4)**: Depends on User Story 1 completion (Feature components reference Atoms/Molecules/Organisms)
- **User Story 3 (Phase 5)**: Depends on User Story 2 completion (import consolidation needs all components in final locations)
- **User Story 4 (Phase 6)**: Depends on User Story 3 completion (hierarchy verification requires all components migrated)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

**IMPORTANT**: This refactoring has SEQUENTIAL dependencies due to component hierarchy:

- **User Story 1 (P1)**: MUST complete before US2 (establishes Atoms/Molecules/Organisms that Features depend on)
- **User Story 2 (P2)**: MUST complete before US3 (Features must be in final location before import consolidation)
- **User Story 3 (P2)**: MUST complete before US4 (imports consolidated before hierarchy verification)
- **User Story 4 (P3)**: Can only run after US1-US3 complete (verifies final hierarchy)

### Within Each User Story

**User Story 1** (Component Consistency):
1. Migrate all Molecules (can run in parallel marked [P])
2. Update molecule namespaces and internal references (sequential)
3. Update page references to use Tb-prefixed molecules (sequential)
4. Migrate all Organisms (can run in parallel marked [P])
5. Consolidate NavigationSidebar (sequential - requires careful merge)
6. Delete duplicates and verify (sequential)

**User Story 2** (Developer Navigation):
1. Create all feature folders (can run in parallel marked [P])
2. Migrate all feature components to folders (can run in parallel marked [P] by feature)
3. Update namespaces per feature (sequential per feature)
4. Update page references (sequential)
5. Delete old folders (sequential)

**User Story 3** (Import Consolidation):
1. Create consolidated _Imports.razor (sequential)
2. Delete duplicate imports (sequential)
3. Clean unused imports (sequential)

**User Story 4** (Atomic Design Hierarchy):
1. Verify dependency rules (can run in parallel marked [P] for different levels)
2. Document hierarchy (sequential)

### Parallel Opportunities

**Within Foundational Phase**:
- T004-T011: All Atom migrations can run in parallel (different files, marked [P])

**Within User Story 1**:
- T014-T022: All Molecule migrations can run in parallel (different files, marked [P])
- T026-T030: Most Organism migrations can run in parallel (different files, marked [P]), except T031 (NavigationSidebar consolidation)

**Within User Story 2**:
- T037-T043: All feature folder creations can run in parallel (marked [P])
- T044-T050: All feature component migrations can run in parallel by feature domain (marked [P])

**Within User Story 4**:
- T075-T078: All dependency verifications can run in parallel (marked [P])

**Within Polish Phase**:
- T081-T083: StyleCop, copyright, and test verifications can run in parallel (marked [P])

---

## Parallel Example: Foundational Phase (Atoms)

```bash
# Launch all Atom migrations together:
Task: "Migrate TbButton: Create subfolder, rename, move ButtonVariant.cs..."
Task: "Migrate TbInput: Rename Input.razor to TbInput.razor..."
Task: "Migrate TbIcon: Create subfolder, rename, move IconName.cs and IconVariant.cs..."
Task: "Migrate TbBadge: Create subfolder, rename, split BadgeEnums.cs..."
Task: "Migrate TbLabel: Create subfolder, rename, move LabelSize.cs..."
Task: "Migrate TbSelect: Rename Select.razor to TbSelect.razor..."
Task: "Migrate TbSpinner: Create subfolder, rename, extract SpinnerSize..."
Task: "Migrate TbToggle: Rename Toggle.razor to TbToggle.razor..."
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T013) - CRITICAL: Atoms must be migrated first
3. Complete Phase 3: User Story 1 (T014-T036) - Molecules, Organisms, duplicate elimination
4. **STOP and VALIDATE**: Verify all components use Tb prefix, test independently
5. Can deploy/demo if this level of consistency is sufficient

### Incremental Delivery

1. Complete Setup + Foundational → Atoms ready (T001-T013)
2. Add User Story 1 → Consistency established (T014-T036) → Deploy/Demo (MVP!)
3. Add User Story 2 → Navigation improved (T037-T068) → Deploy/Demo
4. Add User Story 3 → Imports consolidated (T069-T074) → Deploy/Demo
5. Add User Story 4 → Hierarchy documented (T075-T080) → Deploy/Demo
6. Each story adds value without breaking previous stories

### Full Sequential Strategy

**Due to component hierarchy dependencies, this refactoring MUST be executed sequentially:**

1. Team completes Setup + Foundational together (T001-T013)
2. Team completes User Story 1 together (T014-T036) - Atoms/Molecules/Organisms must exist before Features
3. Team completes User Story 2 together (T037-T068) - Features must be in place before import consolidation
4. Team completes User Story 3 together (T069-T074) - Imports must be consolidated before hierarchy verification
5. Team completes User Story 4 together (T075-T080) - Final verification
6. Complete Polish phase (T081-T086)

**Parallel opportunities exist WITHIN each phase** (tasks marked [P]), but phases themselves are sequential.

---

## Notes

- [P] tasks = different files within same phase, can run in parallel
- [Story] label maps task to specific user story for traceability
- **CRITICAL**: Unlike typical features, this refactoring has SEQUENTIAL user story dependencies due to component hierarchy (Atoms → Molecules → Organisms → Features)
- Existing tests MUST pass unchanged at every checkpoint (no test modifications allowed)
- Commit after each checkpoint for rollback capability
- Stop at any checkpoint to validate independently before proceeding
- StyleCop compliance required throughout - copyright headers must match file names
- Avoid: skipping checkpoints, modifying test logic, introducing functionality changes

---

## Total Task Summary

- **Total Tasks**: 86 tasks
- **Setup**: 3 tasks
- **Foundational**: 10 tasks (8 Atoms + 2 verification)
- **User Story 1** (Component Consistency): 23 tasks (9 Molecules + 6 Organisms + duplicate cleanup)
- **User Story 2** (Developer Navigation): 32 tasks (7 feature folders + 21 component migrations + 4 page updates)
- **User Story 3** (Import Consolidation): 6 tasks
- **User Story 4** (Atomic Design Hierarchy): 6 tasks
- **Polish**: 6 tasks

**Parallel Opportunities**: 46 tasks marked [P] (within phases, not across phases)
**MVP Scope**: Phases 1-3 (T001-T036) = 36 tasks for basic component consistency

**Estimated Timeline** (based on component-migration-map.md):
- Setup: 15 minutes
- Foundational (Atoms): 1.5 hours
- User Story 1 (Molecules + Organisms): 2.5 hours
- User Story 2 (Features + Pages): 2.5 hours
- User Story 3 (Imports): 15 minutes
- User Story 4 (Hierarchy): 30 minutes
- Polish: 1 hour
- **Total**: 8-9 hours (including testing and verification)
