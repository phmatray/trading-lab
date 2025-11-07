# Tasks: UX/UI Enhancement - Navigation & Settings

**Input**: Design documents from `/specs/003-ux-ui-enhancement/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Test tasks are included throughout based on constitution requirements (80% coverage minimum, 100% for critical paths).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Project Structure**: Enhancing existing `src/TradingBot.Web/`, `src/TradingBot.Core/`, `src/TradingBot.Infrastructure/`
- **Tests**: `tests/TradingBot.Web.Tests/`, `tests/TradingBot.Core.Tests/`, `tests/TradingBot.Infrastructure.Tests/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, DaisyUI integration, and database preparation

- [ ] T001 Install DaisyUI via npm in src/TradingBot.Web: `npm install daisyui@latest`
- [ ] T002 [P] Update Tailwind configuration in src/TradingBot.Web/tailwind.config.js to include DaisyUI plugin and themes
- [ ] T003 [P] Create Styles directory structure: src/TradingBot.Web/Styles/components/, src/TradingBot.Web/Styles/utilities/
- [ ] T004 [P] Add theme CSS files: src/TradingBot.Web/wwwroot/css/themes/light.css and dark.css
- [ ] T005 [P] Create JavaScript utilities: src/TradingBot.Web/wwwroot/js/theme-switcher.js and keyboard-shortcuts.js

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database & Domain Layer

- [ ] T006 Create UserPreferences entity in src/TradingBot.Core/Models/UserPreferences.cs with all fields per data-model.md
- [ ] T007 [P] Create NotificationTypesEnabled value object in src/TradingBot.Core/ValueObjects/NotificationTypesEnabled.cs
- [ ] T008 [P] Create IUserPreferencesRepository interface in src/TradingBot.Core/Interfaces/IUserPreferencesRepository.cs
- [ ] T009 Create UserPreferencesConfiguration for EF Core in src/TradingBot.Infrastructure/Persistence/Configurations/UserPreferencesConfiguration.cs
- [ ] T010 Implement UserPreferencesRepository in src/TradingBot.Infrastructure/Persistence/Repositories/UserPreferencesRepository.cs
- [ ] T011 Add UserPreferences DbSet to TradingBotDbContext in src/TradingBot.Infrastructure/Persistence/TradingBotDbContext.cs
- [ ] T012 Create EF Core migration: `dotnet ef migrations add AddUserPreferences --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web`
- [ ] T013 Apply migration: `dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web`

### Services Layer

- [ ] T014 [P] Create IThemeService interface in src/TradingBot.Web/Services/IThemeService.cs
- [ ] T015 [P] Create INotificationService interface in src/TradingBot.Web/Services/INotificationService.cs
- [ ] T016 [P] Create IKeyboardShortcutService interface in src/TradingBot.Web/Services/IKeyboardShortcutService.cs
- [ ] T017 Implement ThemeService in src/TradingBot.Web/Services/ThemeService.cs with JSInterop for theme switching
- [ ] T018 Implement NotificationService in src/TradingBot.Web/Services/NotificationService.cs with observable collection
- [ ] T019 Implement KeyboardShortcutService in src/TradingBot.Web/Services/KeyboardShortcutService.cs with JSInterop
- [ ] T020 Implement PreferencesService in src/TradingBot.Web/Components/Settings/PreferencesService.cs for CRUD operations
- [ ] T021 Register all services in Program.cs dependency injection container

### Shared Components

- [ ] T022 [P] Create Toast.razor component in src/TradingBot.Web/Components/Shared/Toast.razor with DaisyUI alert styling
- [ ] T023 [P] Create Modal.razor component in src/TradingBot.Web/Components/Shared/Modal.razor with focus trap
- [ ] T024 [P] Create Tooltip.razor component in src/TradingBot.Web/Components/Shared/Tooltip.razor with DaisyUI tooltip
- [ ] T025 [P] Create LoadingIndicator.razor component in src/TradingBot.Web/Components/Shared/LoadingIndicator.razor
- [ ] T026 [P] Create HelpIcon.razor component in src/TradingBot.Web/Components/Shared/HelpIcon.razor with tooltip integration

### Foundational Tests

- [ ] T027 [P] Unit test for UserPreferences entity validation in tests/TradingBot.Core.Tests/Models/UserPreferencesTests.cs
- [ ] T028 [P] Repository tests for UserPreferencesRepository in tests/TradingBot.Infrastructure.Tests/Repositories/UserPreferencesRepositoryTests.cs
- [ ] T029 [P] Service tests for ThemeService in tests/TradingBot.Web.Tests/Services/ThemeServiceTests.cs
- [ ] T030 [P] Service tests for NotificationService in tests/TradingBot.Web.Tests/Services/NotificationServiceTests.cs
- [ ] T031 [P] Component tests for Toast in tests/TradingBot.Web.Tests/Components/Shared/ToastTests.cs
- [ ] T032 [P] Component tests for Modal accessibility in tests/TradingBot.Web.Tests/Components/Shared/ModalTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Persistent Navigation Menu (Priority: P1) 🎯 MVP

**Goal**: Implement left sidebar navigation menu that allows users to navigate between all main sections with persistent visibility and collapsible state

**Independent Test**: Navigate through all main sections (Dashboard, Portfolio, Performance, Strategies, Risk Settings, Backtesting) using the left sidebar menu. Verify current page is highlighted. Collapse sidebar and verify icon-only mode works. Expand sidebar and verify labels return.

### Tests for User Story 1

- [ ] T033 [P] [US1] Component test for LeftSidebar rendering in tests/TradingBot.Web.Tests/Components/Layout/LeftSidebarTests.cs
- [ ] T034 [P] [US1] Component test for LeftSidebar collapse/expand behavior in tests/TradingBot.Web.Tests/Components/Layout/LeftSidebarTests.cs
- [ ] T035 [P] [US1] Component test for LeftSidebar active page highlighting in tests/TradingBot.Web.Tests/Components/Layout/LeftSidebarTests.cs

### Implementation for User Story 1

- [ ] T036 [US1] Create LeftSidebar.razor component in src/TradingBot.Web/Components/Layout/LeftSidebar.razor with DaisyUI drawer component
- [ ] T037 [US1] Add navigation items (Dashboard, Portfolio, Performance, Strategies, Risk Settings, Backtesting) with icons to LeftSidebar.razor
- [ ] T038 [US1] Implement collapse/expand toggle logic in LeftSidebar.razor component code
- [ ] T039 [US1] Add active page highlighting logic using NavigationManager in LeftSidebar.razor
- [ ] T040 [US1] Create sidebar CSS styles in src/TradingBot.Web/Styles/components/sidebar.css with transitions
- [ ] T041 [US1] Create TopHeader.razor component in src/TradingBot.Web/Components/Layout/TopHeader.razor for settings access
- [ ] T042 [US1] Modify MainLayout.razor in src/TradingBot.Web/Components/Layout/MainLayout.razor to integrate LeftSidebar and TopHeader
- [ ] T043 [US1] Update MainLayout.razor to handle sidebar collapsed state management
- [ ] T044 [US1] Add responsive behavior for sidebar at minimum 1024px width in sidebar.css

**Checkpoint**: At this point, User Story 1 should be fully functional - users can navigate between all sections using the left sidebar

---

## Phase 4: User Story 2 - User Settings & Preferences (Priority: P1)

**Goal**: Implement settings page where users can configure theme, dashboard refresh interval, notification preferences, and other display settings with persistence across sessions

**Independent Test**: Access settings page from navigation. Modify theme (light/dark), dashboard refresh interval (1-300s), notification types (success/error/info/warning toggles), and notification duration (2-10s). Click "Save Changes" and verify confirmation. Logout and login again to verify settings persisted.

### Tests for User Story 2

- [ ] T045 [P] [US2] Component test for SettingsPage rendering in tests/TradingBot.Web.Tests/Components/Settings/SettingsPageTests.cs
- [ ] T046 [P] [US2] Component test for DisplaySettings in tests/TradingBot.Web.Tests/Components/Settings/DisplaySettingsTests.cs
- [ ] T047 [P] [US2] Component test for NotificationSettings in tests/TradingBot.Web.Tests/Components/Settings/NotificationSettingsTests.cs
- [ ] T048 [P] [US2] Service test for PreferencesService CRUD operations in tests/TradingBot.Web.Tests/Services/PreferencesServiceTests.cs
- [ ] T049 [P] [US2] Integration test for settings persistence in tests/TradingBot.Web.Tests/Integration/SettingsPersistenceTests.cs

### Implementation for User Story 2

- [ ] T050 [P] [US2] Create Settings.razor page in src/TradingBot.Web/Pages/Settings.razor with route @page "/settings"
- [ ] T051 [P] [US2] Create SettingsPage.razor component in src/TradingBot.Web/Components/Settings/SettingsPage.razor as main container
- [ ] T052 [P] [US2] Create DisplaySettings.razor component in src/TradingBot.Web/Components/Settings/DisplaySettings.razor for theme and refresh interval
- [ ] T053 [P] [US2] Create NotificationSettings.razor component in src/TradingBot.Web/Components/Settings/NotificationSettings.razor for notification preferences
- [ ] T054 [US2] Implement theme dropdown with light/dark options in DisplaySettings.razor using DaisyUI select component
- [ ] T055 [US2] Implement dashboard refresh interval input (1-300 seconds) with validation in DisplaySettings.razor
- [ ] T056 [US2] Implement notification type toggles (success/error/info/warning) in NotificationSettings.razor
- [ ] T057 [US2] Implement notification duration input (2-10 seconds) with validation in NotificationSettings.razor
- [ ] T058 [US2] Add "Save Changes" button with loading state in SettingsPage.razor
- [ ] T059 [US2] Add "Reset to Defaults" button with confirmation dialog in SettingsPage.razor
- [ ] T060 [US2] Implement save operation in SettingsPage.razor calling PreferencesService
- [ ] T061 [US2] Implement reset operation in SettingsPage.razor calling PreferencesService
- [ ] T062 [US2] Add unsaved changes warning when navigating away from Settings page
- [ ] T063 [US2] Display success toast notification on settings save using NotificationService
- [ ] T064 [US2] Display error toast notification on settings save failure using NotificationService
- [ ] T065 [US2] Load user preferences on SettingsPage initialization from PreferencesService
- [ ] T066 [US2] Apply theme change immediately on save using ThemeService

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - users can navigate AND configure their preferences

---

## Phase 5: User Story 3 - Enhanced Visual Design & Polish (Priority: P2)

**Goal**: Apply consistent styling, proper spacing, visual feedback (hover states, transitions), loading indicators, success/error notifications, and professional appearance across all pages

**Independent Test**: Navigate through all pages and verify consistent spacing, typography, and color scheme. Hover over buttons, links, and menu items to verify visual feedback appears within 100ms. Trigger actions (save settings, close position) and verify loading indicators appear and success notifications display. Trigger errors and verify error messages display with clear messaging.

### Tests for User Story 3

- [ ] T067 [P] [US3] Visual regression tests for all pages in tests/TradingBot.Web.Tests/Visual/VisualRegressionTests.cs
- [ ] T068 [P] [US3] Performance tests for hover feedback timing in tests/TradingBot.Web.Tests/Performance/VisualFeedbackTests.cs
- [ ] T069 [P] [US3] Component tests for LoadingIndicator in tests/TradingBot.Web.Tests/Components/Shared/LoadingIndicatorTests.cs

### Implementation for User Story 3

- [ ] T070 [P] [US3] Apply DaisyUI component classes to all existing Dashboard components in src/TradingBot.Web/Components/Dashboard/
- [ ] T071 [P] [US3] Apply DaisyUI component classes to all Portfolio components in src/TradingBot.Web/Components/Portfolio/
- [ ] T072 [P] [US3] Apply DaisyUI component classes to all Performance components in src/TradingBot.Web/Components/Performance/
- [ ] T073 [P] [US3] Apply DaisyUI component classes to all Strategy components in src/TradingBot.Web/Components/Strategy/
- [ ] T074 [P] [US3] Apply DaisyUI component classes to all Risk components in src/TradingBot.Web/Components/Risk/
- [ ] T075 [US3] Implement consistent button styling using DaisyUI btn classes across all components
- [ ] T076 [US3] Implement consistent card styling using DaisyUI card classes for all data containers
- [ ] T077 [US3] Implement consistent table styling using DaisyUI table classes with hover states
- [ ] T078 [US3] Add hover transitions (< 100ms) to all interactive elements in src/TradingBot.Web/wwwroot/css/app.css
- [ ] T079 [US3] Add loading indicators to all async operations (save settings, close position, load data) using LoadingIndicator component
- [ ] T080 [US3] Add success toast notifications to all successful operations using NotificationService
- [ ] T081 [US3] Add error toast notifications to all error scenarios with clear messaging using NotificationService
- [ ] T082 [US3] Implement consistent spacing using Tailwind utility classes (p-4, m-2, gap-4) across all components
- [ ] T083 [US3] Implement consistent typography using Tailwind text classes (text-xl, font-bold) across all components
- [ ] T084 [US3] Apply color coding (green for positive P&L, red for negative, yellow for warnings) in all financial displays
- [ ] T085 [US3] Add smooth theme transition animations in src/TradingBot.Web/Styles/components/themes.css

**Checkpoint**: All pages should now have consistent, polished visual design with proper feedback

---

## Phase 6: User Story 4 - Keyboard Navigation & Accessibility (Priority: P2)

**Goal**: Implement full keyboard navigation with shortcuts, ARIA labels, screen reader compatibility, and WCAG 2.1 Level AA compliance for color contrast and focus indicators

**Independent Test**: Navigate through the entire application using only keyboard (Tab, Shift+Tab, Enter, Escape, arrow keys). Verify all interactive elements are accessible, focus indicators are visible, and tab order is logical. Use keyboard shortcuts (Alt+D, Alt+P, Alt+F, Alt+S, Alt+T) to navigate quickly. Test with screen reader (NVDA/VoiceOver) and verify all content is properly announced.

### Tests for User Story 4

- [ ] T086 [P] [US4] Keyboard navigation integration tests in tests/TradingBot.Web.Tests/Accessibility/KeyboardNavigationTests.cs
- [ ] T087 [P] [US4] Tab order tests for all pages in tests/TradingBot.Web.Tests/Accessibility/TabOrderTests.cs
- [ ] T088 [P] [US4] Focus trap tests for modals in tests/TradingBot.Web.Tests/Components/Shared/ModalFocusTrapTests.cs
- [ ] T089 [P] [US4] ARIA label tests in tests/TradingBot.Web.Tests/Accessibility/AriaLabelTests.cs
- [ ] T090 [P] [US4] Color contrast tests using axe-core in tests/TradingBot.Web.Tests/Accessibility/ColorContrastTests.cs

### Implementation for User Story 4

- [ ] T091 [P] [US4] Add ARIA labels to all buttons in all components throughout src/TradingBot.Web/Components/
- [ ] T092 [P] [US4] Add ARIA labels to all form inputs in all components throughout src/TradingBot.Web/Components/
- [ ] T093 [P] [US4] Add ARIA roles to all navigation elements in src/TradingBot.Web/Components/Layout/
- [ ] T094 [US4] Register keyboard shortcuts (Alt+D, Alt+P, Alt+F, Alt+S, Alt+T) in MainLayout.razor using KeyboardShortcutService
- [ ] T095 [US4] Implement global keydown listener in src/TradingBot.Web/wwwroot/js/keyboard-shortcuts.js
- [ ] T096 [US4] Implement focus trap for Modal component in src/TradingBot.Web/Components/Shared/Modal.razor
- [ ] T097 [US4] Ensure modal closes on Escape key and returns focus to triggering element
- [ ] T098 [US4] Add visible focus indicators (:focus-visible) with sufficient contrast in src/TradingBot.Web/Styles/utilities/accessibility.css
- [ ] T099 [US4] Verify and fix tab order across all pages (top-to-bottom, left-to-right logical flow)
- [ ] T100 [US4] Add aria-describedby to all help tooltips in components with HelpIcon
- [ ] T101 [US4] Ensure all form fields support keyboard-only interaction (dropdowns, toggles, date pickers)
- [ ] T102 [US4] Test color contrast ratios (4.5:1 for normal text, 3:1 for large text) and fix any failures
- [ ] T103 [US4] Add keyboard shortcut documentation to help section or tooltip

**Checkpoint**: Application should be fully keyboard accessible and WCAG 2.1 Level AA compliant

---

## Phase 7: User Story 5 - Responsive Layouts & Component Consistency (Priority: P3)

**Goal**: Ensure all pages and components have consistent layouts, headers, footers, navigation placement, and work well across different desktop screen sizes (1024px to ultra-wide)

**Independent Test**: View all pages at different screen resolutions (1024px, 1280px, 1920px, 2560px). Verify layouts adapt smoothly without horizontal scrolling or broken layouts down to 1024px width. Compare pages and verify consistent header, footer, navigation placement, and content area structure. Verify all data cards, forms, charts have consistent styling.

### Tests for User Story 5

- [ ] T104 [P] [US5] Responsive layout tests for all pages at 1024px, 1280px, 1920px in tests/TradingBot.Web.Tests/Responsive/ResponsiveLayoutTests.cs
- [ ] T105 [P] [US5] Component consistency tests comparing styling across pages in tests/TradingBot.Web.Tests/Visual/ConsistencyTests.cs

### Implementation for User Story 5

- [ ] T106 [P] [US5] Implement max-width constraint (max-w-7xl) for main content area in MainLayout.razor
- [ ] T107 [P] [US5] Add responsive breakpoint classes (lg:) for sidebar behavior at 1024px in sidebar.css
- [ ] T108 [US5] Ensure consistent header structure across all pages in src/TradingBot.Web/Pages/
- [ ] T109 [US5] Ensure consistent footer structure across all pages (if applicable)
- [ ] T110 [US5] Standardize data card component styling across Dashboard, Portfolio, Performance pages
- [ ] T111 [US5] Standardize form element styling (inputs, buttons, labels, validation) across Settings and Risk pages
- [ ] T112 [US5] Standardize chart styling across Performance and Backtest pages
- [ ] T113 [US5] Test and fix any horizontal scrolling issues at 1024px width
- [ ] T114 [US5] Test and fix any layout breaking at ultra-wide resolutions (2560px+)
- [ ] T115 [US5] Document responsive behavior and breakpoints in quickstart.md

**Checkpoint**: All pages should have consistent layout and work well across desktop screen sizes

---

## Phase 8: User Story 6 - Contextual Help & User Guidance (Priority: P3)

**Goal**: Add helpful tooltips, explanations, and guidance throughout the application to help users understand features and metrics without consulting external documentation

**Independent Test**: Navigate through all pages and identify complex metrics (Sharpe Ratio, Sortino, risk settings). Hover over or click help icons next to these metrics and verify clear, concise explanations appear in tooltips. Encounter empty states (no positions, no trades) and verify helpful guidance is displayed. Trigger form validation errors and verify specific guidance on how to correct input is provided.

### Tests for User Story 6

- [ ] T116 [P] [US6] Component tests for HelpIcon with tooltip in tests/TradingBot.Web.Tests/Components/Shared/HelpIconTests.cs
- [ ] T117 [P] [US6] Tooltip content tests verifying clarity in tests/TradingBot.Web.Tests/Components/Shared/TooltipContentTests.cs

### Implementation for User Story 6

- [ ] T118 [P] [US6] Add HelpIcon with tooltip to Sharpe Ratio metric in src/TradingBot.Web/Components/Performance/PerformanceMetrics.razor
- [ ] T119 [P] [US6] Add HelpIcon with tooltip to Sortino Ratio metric in src/TradingBot.Web/Components/Performance/PerformanceMetrics.razor
- [ ] T120 [P] [US6] Add HelpIcon with tooltip to Calmar Ratio metric in src/TradingBot.Web/Components/Performance/PerformanceMetrics.razor
- [ ] T121 [P] [US6] Add HelpIcon with tooltip to Max Drawdown metric in src/TradingBot.Web/Components/Performance/PerformanceMetrics.razor
- [ ] T122 [P] [US6] Add HelpIcon with tooltip to each risk setting in src/TradingBot.Web/Components/Risk/RiskSettings.razor
- [ ] T123 [US6] Add helpful empty state message to PositionList when no positions in src/TradingBot.Web/Components/Dashboard/PositionList.razor
- [ ] T124 [US6] Add helpful empty state message to TradeHistory when no trades in src/TradingBot.Web/Components/Portfolio/TradeHistory.razor
- [ ] T125 [US6] Add specific guidance to all form validation errors in Settings and Risk components
- [ ] T126 [US6] Create help section or FAQ page in src/TradingBot.Web/Pages/Help.razor with common tasks
- [ ] T127 [US6] Add help section to navigation menu in LeftSidebar.razor
- [ ] T128 [US6] Create tooltip content resource file for easy maintenance in src/TradingBot.Web/Resources/TooltipContent.resx
- [ ] T129 [US6] Implement dynamic tooltip positioning (top/bottom/left/right) based on available space in Tooltip.razor

**Checkpoint**: All complex metrics, settings, and empty states should have clear, helpful guidance

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements, optimization, and validation that affect multiple user stories

### Performance Optimization

- [ ] T130 [P] Implement caching for UserPreferences in memory using IMemoryCache in PreferencesService
- [ ] T131 [P] Implement debouncing for settings save operations (1 second delay) in SettingsPage.razor
- [ ] T132 [P] Lazy load theme CSS (only active theme) in MainLayout.razor
- [ ] T133 [P] Minimize JSInterop calls by batching theme and keyboard shortcut registrations

### Security & Validation

- [ ] T134 [P] Verify input validation on all settings (refresh interval 1-300s, notification duration 2-10s) with error messages
- [ ] T135 [P] Verify authorization ensures users can only access their own preferences (UserId filter in repository)
- [ ] T136 [P] Add logging for all settings changes with user ID and timestamp using Serilog

### Testing & Quality Assurance

- [ ] T137 [P] Run full test suite: `dotnet test`
- [ ] T138 [P] Verify code coverage meets 80% minimum using `dotnet test --collect:"XPlat Code Coverage"`
- [ ] T139 [P] Verify critical path coverage is 100% (settings persistence, theme switching, navigation) using coverage report
- [ ] T140 [P] Run code analyzers: `dotnet build /p:RunAnalyzers=true`
- [ ] T141 [P] Test keyboard navigation on all pages manually
- [ ] T142 [P] Test screen reader compatibility using NVDA (Windows) or VoiceOver (macOS)
- [ ] T143 [P] Verify WCAG 2.1 Level AA compliance using axe DevTools or Lighthouse
- [ ] T144 [P] Performance testing: verify settings save < 1s, visual feedback < 100ms, theme change instant

### Documentation

- [ ] T145 [P] Update quickstart.md with any implementation changes or new patterns discovered
- [ ] T146 [P] Add inline XML documentation comments to all public APIs (services, repositories)
- [ ] T147 [P] Update CLAUDE.md Active Technologies section with DaisyUI and user preferences implementation

### Final Validation

- [ ] T148 Run quickstart.md validation by following 5-minute quick start guide
- [ ] T149 Validate all user stories work independently (US1 through US6)
- [ ] T150 Validate settings persistence across sessions (logout/login test)
- [ ] T151 Validate theme switching works correctly without page reload
- [ ] T152 Validate all keyboard shortcuts (Alt+D, Alt+P, Alt+F, Alt+S, Alt+T) work correctly
- [ ] T153 Final deployment checklist verification from quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P1 → P2 → P2 → P3 → P3)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1 - Navigation)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P1 - Settings)**: Can start after Foundational - No dependencies on other stories, but integrates with US1 (uses navigation)
- **User Story 3 (P2 - Visual Polish)**: Depends on US1 and US2 being complete to apply styling to existing components
- **User Story 4 (P2 - Accessibility)**: Can start after Foundational - Enhances all stories but independently testable
- **User Story 5 (P3 - Responsive)**: Depends on US1-4 to ensure consistency across all components
- **User Story 6 (P3 - Help)**: Depends on US1-5 to add tooltips to all existing metrics and components

### Within Each User Story

- Tests MUST be written and PASS before marking story complete
- Shared components (Toast, Modal, Tooltip) before story-specific components
- Domain entities (UserPreferences) before repositories before services
- Services before page components
- Core implementation before integration with other stories
- Story complete before moving to next priority

### Parallel Opportunities

**Foundational Phase (T006-T032)**: Can be split into parallel streams:
- Stream 1: Database layer (T006-T013)
- Stream 2: Services interfaces and implementations (T014-T021)
- Stream 3: Shared components (T022-T026)
- Stream 4: Tests for above (T027-T032)

**User Story 1 (T033-T044)**: Tests (T033-T035) can run in parallel, then components can be built in parallel (LeftSidebar, TopHeader)

**User Story 2 (T045-T066)**: Tests (T045-T049) in parallel, then components (T050-T053) in parallel, then integration

**User Story 3 (T067-T085)**: Applying DaisyUI classes to different component directories (T070-T074) can all run in parallel

**User Story 4 (T086-T103)**: Tests (T086-T090) in parallel, ARIA labels across different directories (T091-T093) in parallel

**User Story 5 (T104-T115)**: Tests in parallel, responsive fixes across different pages can run in parallel

**User Story 6 (T116-T129)**: Adding HelpIcons to different components (T118-T122, T123-T124) can run in parallel

**Polish Phase (T130-T153)**: Performance tasks, security tasks, testing tasks, and documentation tasks can all run in parallel

---

## Parallel Example: Foundational Phase

```bash
# Stream 1: Database layer
Task: "Create UserPreferences entity in src/TradingBot.Core/Models/UserPreferences.cs"
Task: "Create NotificationTypesEnabled value object"
Task: "Create IUserPreferencesRepository interface"
→ Then: "Create UserPreferencesConfiguration for EF Core"
→ Then: "Implement UserPreferencesRepository"
→ Then: "Add DbSet to TradingBotDbContext"
→ Then: "Create and apply EF Core migration"

# Stream 2: Services (parallel with Stream 1)
Task: "Create IThemeService interface"
Task: "Create INotificationService interface"
Task: "Create IKeyboardShortcutService interface"
→ Then: "Implement ThemeService"
→ Then: "Implement NotificationService"
→ Then: "Implement KeyboardShortcutService"
→ Then: "Register services in DI"

# Stream 3: Shared Components (parallel with Streams 1 & 2)
Task: "Create Toast.razor component"
Task: "Create Modal.razor component"
Task: "Create Tooltip.razor component"
Task: "Create LoadingIndicator.razor component"
Task: "Create HelpIcon.razor component"

# Stream 4: Tests (parallel with Streams 1, 2, & 3)
Task: "Unit test for UserPreferences validation"
Task: "Repository tests for UserPreferencesRepository"
Task: "Service tests for ThemeService"
Task: "Service tests for NotificationService"
Task: "Component tests for Toast"
Task: "Component tests for Modal accessibility"
```

---

## Parallel Example: User Story 3 (Visual Polish)

```bash
# Apply DaisyUI classes to different component directories in parallel:
Task: "Apply DaisyUI to Dashboard components in src/TradingBot.Web/Components/Dashboard/"
Task: "Apply DaisyUI to Portfolio components in src/TradingBot.Web/Components/Portfolio/"
Task: "Apply DaisyUI to Performance components in src/TradingBot.Web/Components/Performance/"
Task: "Apply DaisyUI to Strategy components in src/TradingBot.Web/Components/Strategy/"
Task: "Apply DaisyUI to Risk components in src/TradingBot.Web/Components/Risk/"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only - Both P1)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundational (T006-T032) - CRITICAL - blocks all stories
3. Complete Phase 3: User Story 1 - Navigation (T033-T044)
4. Complete Phase 4: User Story 2 - Settings (T045-T066)
5. **STOP and VALIDATE**: Test US1 and US2 independently and together
6. Run tests, verify 80% coverage minimum
7. Deploy/demo if ready - Users can navigate and configure preferences

**MVP Delivered**: Users can navigate the application using a left sidebar menu and configure their personal preferences (theme, refresh intervals, notifications) with persistence.

### Incremental Delivery

1. Complete Setup + Foundational (T001-T032) → Foundation ready
2. Add User Story 1 (T033-T044) → Test independently → Navigation works!
3. Add User Story 2 (T045-T066) → Test independently → Settings work!
4. **MVP Complete** - Deploy if desired
5. Add User Story 3 (T067-T085) → Test independently → Visual polish applied!
6. Add User Story 4 (T086-T103) → Test independently → Fully accessible!
7. Add User Story 5 (T104-T115) → Test independently → Consistent layouts!
8. Add User Story 6 (T116-T129) → Test independently → Helpful guidance!
9. Complete Polish (T130-T153) → Full validation → Production ready!

Each story adds value without breaking previous stories.

### Parallel Team Strategy

With multiple developers:

1. **Week 1**: Team completes Setup + Foundational together (T001-T032)
2. **Week 2**: Once Foundational is done:
   - Developer A: User Story 1 - Navigation (T033-T044)
   - Developer B: User Story 2 - Settings (T045-T066)
3. **Week 3**: Once US1 & US2 are done:
   - Developer A: User Story 3 - Visual Polish (T067-T085)
   - Developer B: User Story 4 - Accessibility (T086-T103)
4. **Week 4**: Final stories and polish:
   - Developer A: User Story 5 - Responsive (T104-T115)
   - Developer B: User Story 6 - Help (T116-T129)
5. **Week 5**: Both developers on Polish & Validation (T130-T153)

Stories complete and integrate independently. MVP can be deployed after Week 2.

---

## Notes

- [P] tasks = different files, no dependencies - can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Tests are included throughout (constitution requires 80% coverage, 100% for critical paths)
- US3 (Visual Polish) is the first story with dependencies on US1 & US2 as it applies styling to their components
- US5 (Responsive) and US6 (Help) depend on previous stories to ensure consistency
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- Database migrations must be created and applied in Foundational phase before any user story work begins
- DaisyUI components should be used throughout for consistency and accessibility
- All keyboard shortcuts must be documented for users
- Theme switching must be instant without page reload
- Settings must persist across sessions (logout/login test is critical)

---

## Task Count Summary

- **Phase 1 (Setup)**: 5 tasks
- **Phase 2 (Foundational)**: 27 tasks (T006-T032)
- **Phase 3 (US1 - Navigation)**: 12 tasks (T033-T044)
- **Phase 4 (US2 - Settings)**: 22 tasks (T045-T066)
- **Phase 5 (US3 - Visual Polish)**: 19 tasks (T067-T085)
- **Phase 6 (US4 - Accessibility)**: 18 tasks (T086-T103)
- **Phase 7 (US5 - Responsive)**: 12 tasks (T104-T115)
- **Phase 8 (US6 - Help)**: 14 tasks (T116-T129)
- **Phase 9 (Polish)**: 24 tasks (T130-T153)

**Total**: 153 tasks

**MVP Scope** (US1 + US2): 66 tasks (T001-T066)
**Full Implementation**: 153 tasks
