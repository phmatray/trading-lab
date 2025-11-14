# Implementation Plan: Component Refactoring and Organization

**Branch**: `004-component-refactor` | **Date**: 2025-01-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-component-refactor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature refactors the TradingBot.Web Blazor Server application to implement Atomic Design principles with a "Tb" prefix for all custom components. The refactoring reorganizes 51 components and 7 pages into a logical hierarchy (Atoms, Molecules, Organisms, Features) while consolidating duplicate components, co-locating supporting types, and eliminating unused imports. The technical approach uses C# file operations, namespace updates, and Blazor component migration while maintaining 100% functional parity verified by existing bUnit tests.

## Technical Context

**Language/Version**: C# / .NET 9 with ASP.NET Core Blazor Server
**Primary Dependencies**: Tailwind CSS 3.x for styling, bUnit for component testing, Blazor Server runtime
**Storage**: N/A (pure UI refactoring, no data storage changes)
**Testing**: bUnit (Blazor component testing), xUnit (test framework), existing test suite must pass unchanged
**Target Platform**: Desktop web browsers (minimum 1024px viewport, Blazor Server SSR)
**Project Type**: Web application (Blazor Server with Components and Pages folders)
**Performance Goals**: No performance degradation, maintain existing page load times and rendering performance
**Constraints**: Zero functionality changes, all existing tests must pass without modification, must complete in single deployable unit
**Scale/Scope**: 51 components + 7 pages to refactor, approximately 15-20 supporting enum types to relocate

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Code Quality Principles
- ✅ **Single Responsibility**: Each component has one clear purpose (refactoring maintains existing responsibilities)
- ✅ **DRY Principle**: Eliminates duplicate components (Button, NavMenu duplicates removed)
- ✅ **C# Standards**: PascalCase for components maintained, nullable reference types preserved
- ✅ **Code Organization**: Implements clear layered architecture (Atoms → Molecules → Organisms)
- ✅ **Documentation**: Copyright headers preserved, component hierarchy will be documented

### Testing Standards
- ✅ **Test Coverage**: Existing 80%+ coverage maintained, zero tests require changes
- ✅ **Test Quality**: AAA pattern preserved in existing tests, all tests pass unchanged
- ✅ **Test Independence**: No test dependencies affected by refactoring
- ✅ **Fast Execution**: No performance impact on test suite execution

### User Experience Consistency
- ✅ **Consistency**: Tb-prefix ensures uniform component naming across application
- ✅ **Accessibility**: WCAG 2.1 Level AA compliance preserved (no behavioral changes)
- ✅ **Responsive Design**: Desktop-first approach maintained (no layout changes)
- ✅ **Atomic Design**: Implements recommended Atoms/Molecules/Organisms pattern from constitution

### Performance Requirements
- ✅ **Response Times**: No impact on API endpoints or page load times (pure refactoring)
- ✅ **Resource Optimization**: No memory or network changes (file organization only)
- ✅ **Monitoring**: Existing logging and metrics unaffected

### Security Standards
- ✅ **No Security Impact**: Pure refactoring with zero authentication, authorization, or data protection changes
- ✅ **Input Validation**: Preserved in existing components

### DevOps and CI/CD
- ✅ **Version Control**: Single feature branch (004-component-refactor)
- ✅ **Automated Tests**: All existing tests must pass in CI pipeline
- ✅ **Static Analysis**: StyleCop compliance maintained with updated copyright headers

### Code Review Standards
- ✅ **Review Checklist**: Will verify zero functionality changes (pure refactoring validation)

**GATE STATUS**: ✅ PASS - No constitution violations. This is a pure refactoring effort that improves code organization while maintaining all quality standards.

## Project Structure

### Documentation (this feature)

```text
specs/004-component-refactor/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── component-migration-map.md
│   └── namespace-mapping.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Current Structure** (Before Refactoring):
```text
src/TradingBot.Web/
├── Components/
│   ├── Atoms/           # 8 basic components (Button, Input, Icon, Badge, etc.)
│   ├── Molecules/       # 9 composite components (Card, Modal, Table, etc.)
│   ├── Organisms/       # 6 complex sections (NavigationSidebar, ToastContainer, etc.)
│   ├── Shared/          # 6 legacy components (DUPLICATE - to be removed)
│   ├── Pages/           # 2 page components (MISPLACED - move to /Pages)
│   ├── Layout/          # 2 layout components (MainLayout, NavMenu)
│   ├── Dashboard/       # 6 feature components
│   ├── Portfolio/       # 4 feature components
│   ├── Strategy/        # 2 feature components
│   ├── Risk/            # 2 feature components
│   ├── Performance/     # 2 feature components
│   ├── Backtest/        # 3 feature components
│   ├── Charts/          # 2 feature components
│   └── _Imports.razor   # Component-level imports (DUPLICATE)
├── Pages/
│   ├── _Imports.razor   # Page-level imports (DUPLICATE)
│   ├── Home.razor       # 5 existing pages
│   ├── Dashboard.razor
│   ├── Portfolio.razor
│   ├── Strategies.razor
│   └── Backtest.razor
├── Models/
│   └── [Component enums to be moved to component folders]
└── Services/            # Unaffected by this refactoring

```

**Target Structure** (After Refactoring):
```text
src/TradingBot.Web/
├── Components/
│   ├── _Imports.razor                    # SINGLE consolidated import file
│   ├── Atoms/                            # Basic UI primitives (Tb-prefixed)
│   │   ├── TbButton/
│   │   │   ├── TbButton.razor
│   │   │   └── ButtonVariant.cs         # Co-located enum
│   │   ├── TbInput.razor
│   │   ├── TbIcon/
│   │   │   ├── TbIcon.razor
│   │   │   ├── IconName.cs
│   │   │   └── IconVariant.cs
│   │   ├── TbBadge/
│   │   ├── TbLabel/
│   │   ├── TbSelect.razor
│   │   ├── TbSpinner/
│   │   └── TbToggle.razor
│   ├── Molecules/                        # Composite components
│   │   ├── TbCard.razor
│   │   ├── TbModal.razor
│   │   ├── TbTable.razor
│   │   ├── TbFormField.razor
│   │   ├── TbMenuItem.razor
│   │   ├── TbToast/
│   │   ├── TbPageHeader.razor
│   │   └── TbInfoTooltip/
│   ├── Organisms/                        # Complex sections
│   │   ├── TbNavigationSidebar.razor    # Consolidated NavMenu + NavigationSidebar
│   │   ├── TbToastContainer.razor
│   │   ├── TbErrorBoundary.razor
│   │   ├── TbNotificationCenter.razor
│   │   ├── TbSettingsForm.razor
│   │   └── TbThemeProvider.razor
│   ├── Features/                         # Feature-specific components
│   │   ├── Dashboard/
│   │   │   ├── TbDashboardHeader.razor
│   │   │   ├── TbAccountSummaryCard.razor
│   │   │   ├── TbPerformanceMetricsCard.razor
│   │   │   ├── TbActiveStrategiesCard.razor
│   │   │   ├── TbRecentTradesCard.razor
│   │   │   └── TbMarketOverviewCard.razor
│   │   ├── Portfolio/
│   │   │   ├── TbPortfolioSummary.razor
│   │   │   ├── TbPositionCard.razor
│   │   │   ├── TbPortfolioChart.razor
│   │   │   └── TbAssetAllocationChart.razor
│   │   ├── Strategy/
│   │   │   ├── TbStrategyCard.razor
│   │   │   └── TbStrategyConfigForm.razor
│   │   ├── Risk/
│   │   │   ├── TbRiskMetricsCard.razor
│   │   │   └── TbRiskLimitsForm.razor
│   │   ├── Performance/
│   │   │   ├── TbEquityCurveChart.razor
│   │   │   └── TbPerformanceStatsCard.razor
│   │   ├── Backtest/
│   │   │   ├── TbBacktestConfigForm.razor
│   │   │   ├── TbBacktestResultsCard.razor
│   │   │   └── TbBacktestChart.razor
│   │   └── Charts/
│   │       ├── TbCandlestickChart.razor
│   │       └── TbLineChart.razor
│   └── Layout/
│       └── MainLayout.razor              # Blazor convention, no Tb prefix
├── Pages/
│   ├── Home.razor                        # All 7 pages consolidated here
│   ├── Dashboard.razor
│   ├── Portfolio.razor
│   ├── Strategies.razor
│   ├── Backtest.razor
│   ├── Settings.razor                    # Moved from Components/Pages
│   └── Help.razor                        # Moved from Components/Pages
├── Models/                                # Component-specific enums removed
└── Services/                              # Unaffected
```

**Structure Decision**: This refactoring implements a Blazor Server web application structure following Atomic Design principles. Components are organized in a clear hierarchy (Atoms → Molecules → Organisms → Features) with domain-specific components in dedicated feature folders. Supporting types are co-located with components, and a single consolidated _Imports.razor eliminates duplication.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations detected** - This refactoring aligns with all constitution requirements.

---

## Phase 2: Post-Design Constitution Re-evaluation

### Re-evaluation Status: ✅ PASS

After completing the design phase (research, data model, contracts, and quickstart guide), we re-evaluate the Constitution Check to ensure the design maintains compliance.

#### Code Quality Principles - Re-evaluation
- ✅ **Single Responsibility**: Component hierarchy enforces clear responsibilities (Atoms for primitives, Molecules for composition, Organisms for complex sections, Features for domain logic)
- ✅ **DRY Principle**: Migration map eliminates 7 duplicate components, establishing single source of truth for each component
- ✅ **C# Standards**: Namespace mapping ensures consistent PascalCase, nullable reference types preserved throughout
- ✅ **Code Organization**: Data model defines clear 4-level hierarchy with explicit dependency rules (no circular dependencies verified)
- ✅ **Documentation**: research.md, data-model.md, quickstart.md provide comprehensive documentation

#### Testing Standards - Re-evaluation
- ✅ **Test Coverage**: Research confirms bUnit testing approach with xUnit inheritance pattern, maintaining 80%+ coverage
- ✅ **Test Quality**: Example test patterns in quickstart.md follow AAA pattern and maintain existing test structure
- ✅ **Test Independence**: Migration strategy ensures tests pass without modification (pure refactoring validation)
- ✅ **Fast Execution**: No performance changes to test suite (file organization only)

#### User Experience Consistency - Re-evaluation
- ✅ **Consistency**: Tb-prefix convention documented in quickstart.md with comprehensive usage examples
- ✅ **Accessibility**: Research confirms WCAG 2.1 Level AA preservation (no behavioral changes)
- ✅ **Responsive Design**: Tailwind CSS utility-first approach maintained per research findings
- ✅ **Atomic Design**: Data model provides complete component hierarchy with classification decision tree

#### Performance Requirements - Re-evaluation
- ✅ **Response Times**: No impact verified (pure refactoring with zero functionality changes)
- ✅ **Resource Optimization**: File organization has no runtime impact
- ✅ **Monitoring**: No changes to existing logging or metrics infrastructure

#### Security Standards - Re-evaluation
- ✅ **No Security Impact**: Confirmed in technical context - pure UI refactoring with zero data protection changes
- ✅ **Input Validation**: All existing validation preserved in components (no behavioral changes)

#### DevOps and CI/CD - Re-evaluation
- ✅ **Version Control**: Migration map defines 7 incremental checkpoints with rollback instructions
- ✅ **Automated Tests**: Verification checklist ensures all existing tests pass at each checkpoint
- ✅ **Static Analysis**: Migration map includes StyleCop verification at each phase

#### Code Review Standards - Re-evaluation
- ✅ **Review Checklist**: Component migration map provides detailed verification checklist ensuring zero functionality changes

### Design Validation Summary

| Aspect | Phase 1 Assessment | Phase 2 Re-evaluation | Status |
|--------|-------------------|----------------------|--------|
| Code Quality | ✅ Pass | ✅ Pass | No change |
| Testing | ✅ Pass | ✅ Pass | No change |
| UX Consistency | ✅ Pass | ✅ Pass | No change |
| Performance | ✅ Pass | ✅ Pass | No change |
| Security | ✅ Pass | ✅ Pass | No change |
| DevOps | ✅ Pass | ✅ Pass | No change |
| Code Review | ✅ Pass | ✅ Pass | No change |

**Final Verdict**: ✅ **APPROVED FOR IMPLEMENTATION**

The design artifacts (research.md, data-model.md, contracts/, quickstart.md) fully support the constitution requirements. The refactoring strategy is sound, well-documented, and maintains all quality standards.

---

## Next Steps

Planning phase is complete. Proceed to:

1. **Phase 2**: Generate `tasks.md` using `/speckit.tasks` command
2. **Implementation**: Execute tasks using `/speckit.implement` command or manual implementation following the migration map

All design artifacts are ready for implementation:
- ✅ `plan.md` - Complete implementation plan
- ✅ `research.md` - Technology best practices and decisions
- ✅ `data-model.md` - Complete component hierarchy with 44 components
- ✅ `contracts/component-migration-map.md` - Detailed step-by-step migration instructions
- ✅ `contracts/namespace-mapping.md` - Complete namespace update guide
- ✅ `quickstart.md` - Developer quickstart guide
- ✅ Agent context updated (CLAUDE.md)
