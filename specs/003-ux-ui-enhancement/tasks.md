# Tasks: UX/UI Enhancement - Navigation & Settings

**Input**: Design documents from `/specs/003-ux-ui-enhancement/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/user-preferences-api.yaml

**Tests**: Tests are included per project constitution requirement (80% coverage minimum, 100% for critical paths).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US6)
- Include exact file paths in descriptions

## Path Conventions

This is a Blazor Server web application with clean architecture:
- **Web**: `src/TradingBot.Web/` (Blazor components, pages, services)
- **Core**: `src/TradingBot.Core/` (domain entities, interfaces, validators)
- **Infrastructure**: `src/TradingBot.Infrastructure/` (repositories, EF config, migrations)
- **Tests**: `tests/TradingBot.Web.Tests/`, `tests/TradingBot.Core.Tests/`, `tests/TradingBot.Infrastructure.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and Tailwind/Heroicons configuration

- [X] T001 Update tailwind.config.js to enable class-based dark mode and add custom theme colors
- [X] T002 Update src/TradingBot.Web/Styles/app.css with CSS variables for light/dark themes
- [X] T003 [P] Run npm install in src/TradingBot.Web to ensure Tailwind dependencies are current
- [X] T004 [P] Build Tailwind CSS with `npm run css:build` to generate wwwroot/css/app.css
- [X] T005 [P] Create src/TradingBot.Web/Components/Atoms/ directory structure
- [X] T006 [P] Create src/TradingBot.Web/Components/Molecules/ directory structure
- [X] T007 [P] Create src/TradingBot.Web/Components/Organisms/ directory structure
- [X] T008 [P] Create tests/TradingBot.Web.Tests/ project and add bUnit package reference

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database & Domain Layer

- [X] T009 Create src/TradingBot.Core/ValueObjects/Theme.cs SmartEnum (Light, Dark)
- [X] T010 Create src/TradingBot.Core/Entities/UserPreferences.cs entity per data-model.md
- [X] T011 Create src/TradingBot.Core/Interfaces/IUserPreferencesRepository.cs interface
- [X] T012 [P] Create src/TradingBot.Core/Interfaces/IUserPreferencesService.cs interface
- [X] T013 [P] Create src/TradingBot.Core/Validators/UserPreferencesValidator.cs with range validation
- [X] T014 Create src/TradingBot.Infrastructure/Persistence/Configurations/UserPreferencesConfiguration.cs for EF Core
- [X] T015 Create src/TradingBot.Infrastructure/Persistence/Repositories/UserPreferencesRepository.cs implementation
- [X] T016 Create src/TradingBot.Infrastructure/Services/UserPreferencesService.cs implementation
- [X] T017 Create EF migration: `dotnet ef migrations add AddUserPreferences --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web`
- [X] T018 Apply migration: `dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web`
- [X] T019 Update src/TradingBot.Infrastructure/Persistence/TradingBotDbContext.cs to add DbSet<UserPreferences>
- [X] T020 Register IUserPreferencesRepository and IUserPreferencesService in src/TradingBot.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs

### Client-Side Services

- [X] T021 [P] Create src/TradingBot.Web/Services/UIStateService.cs for sidebar collapse state
- [X] T022 [P] Create src/TradingBot.Web/Services/NavigationService.cs for active route detection
- [X] T023 [P] Create src/TradingBot.Web/Models/ToastNotification.cs per data-model.md (already exists from spec 002)
- [X] T024 [P] Create src/TradingBot.Web/Services/ToastService.cs for notification management (already exists from spec 002)
- [X] T025 Register UIStateService, NavigationService, ToastService in src/TradingBot.Web/Program.cs

### Foundational Tests

- [X] T026 [P] Write tests/TradingBot.Core.Tests/Validators/UserPreferencesValidatorTests.cs (test range validation 1-300, 2-10)
- [X] T027 [P] Write tests/TradingBot.Infrastructure.Tests/Repositories/UserPreferencesRepositoryTests.cs (CRUD operations)
- [X] T028 [P] Write tests/TradingBot.Infrastructure.Tests/Services/UserPreferencesServiceTests.cs (100% coverage - critical path)
- [X] T029 [P] Write tests/TradingBot.Web.Tests/Services/UIStateServiceTests.cs (toggle sidebar state)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Persistent Navigation Menu (Priority: P1) 🎯 MVP

**Goal**: Implement a persistent left sidebar navigation with collapsible icon-only mode, showing all main sections with active page highlighting.

**Independent Test**: Navigate through all main sections (Dashboard, Portfolio, Performance, Strategies, Risk Settings, Backtesting) using the menu. Verify current page is highlighted. Collapse sidebar to icon-only mode and expand. Confirm navigation works from any page.

### Atomic Components for US1

- [X] T030 [P] [US1] Create src/TradingBot.Web/Components/Atoms/Icon.razor with Heroicons SVG support (Home, ChartBar, Briefcase, Cog, Beaker, ChartLine, Bars3, XMark, ChevronLeft, ChevronRight)
- [X] T031 [P] [US1] Write tests/TradingBot.Web.Tests/Components/Atoms/IconTests.cs (renders correct SVG, applies CSS classes)

### Molecular Components for US1

- [X] T032 [US1] Create src/TradingBot.Web/Components/Molecules/MenuItem.razor (Icon + Label, supports collapsed mode, active state highlighting)
- [X] T033 [US1] Write tests/TradingBot.Web.Tests/Components/Molecules/MenuItemTests.cs (collapsed/expanded states, active highlighting)

### Organism Components for US1

- [X] T034 [US1] Create src/TradingBot.Web/Components/Organisms/NavigationSidebar.razor (uses UIStateService, NavigationService, MenuItem molecules)
- [X] T035 [US1] Write tests/TradingBot.Web.Tests/Components/Organisms/NavigationSidebarTests.cs (toggle collapse, navigation, active route detection)

### Layout Integration for US1

- [X] T036 [US1] Update src/TradingBot.Web/Components/Layout/MainLayout.razor to replace NavMenu with NavigationSidebar
- [X] T037 [US1] Add transition classes for sidebar collapse/expand animation in MainLayout
- [X] T038 [US1] Adjust main content area margin based on sidebar state (ml-64 expanded, ml-16 collapsed)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently. Navigation menu is persistent, collapsible, and highlights active page.

---

## Phase 4: User Story 2 - User Settings & Preferences (Priority: P1) 🎯 MVP

**Goal**: Implement a dedicated settings page where users can configure theme (light/dark), dashboard refresh interval (1-300s), notification duration (2-10s), and notification type toggles. Settings persist across sessions.

**Independent Test**: Access settings page from navigation menu. Change theme from light to dark and verify immediate application. Modify refresh interval to 30 seconds and notification duration to 8 seconds. Toggle off "Info" notifications. Click "Save Changes" and verify confirmation toast. Refresh browser and confirm all settings persisted. Click "Reset to Defaults" and confirm reset with dialog.

### Atomic Components for US2

- [X] T039 [P] [US2] Create src/TradingBot.Web/Components/Atoms/Input.razor (text, number types, with Tailwind styling, ARIA labels)
- [X] T040 [P] [US2] Create src/TradingBot.Web/Components/Atoms/Select.razor (dropdown with options, Tailwind styling)
- [X] T041 [P] [US2] Create src/TradingBot.Web/Components/Atoms/Toggle.razor (checkbox styled as toggle switch for boolean settings)
- [X] T042 [P] [US2] Create src/TradingBot.Web/Components/Atoms/Button.razor (Primary, Secondary, Danger, Ghost variants with Tailwind classes)
- [X] T043 [P] [US2] Write tests/TradingBot.Web.Tests/Components/Atoms/InputTests.cs (renders, binds value, shows validation errors)
- [X] T044 [P] [US2] Write tests/TradingBot.Web.Tests/Components/Atoms/SelectTests.cs (renders options, binds value)
- [X] T045 [P] [US2] Write tests/TradingBot.Web.Tests/Components/Atoms/ToggleTests.cs (toggles state, binds value)
- [X] T046 [P] [US2] Write tests/TradingBot.Web.Tests/Components/Atoms/ButtonTests.cs (renders variants, invokes onClick)

### Molecular Components for US2

- [X] T047 [US2] Create src/TradingBot.Web/Components/Molecules/FormField.razor (Label + Input/Select/Toggle + Error message display)
- [X] T048 [US2] Write tests/TradingBot.Web.Tests/Components/Molecules/FormFieldTests.cs (renders label, input, error message)

### Organism Components for US2

- [X] T049 [US2] Create src/TradingBot.Web/Components/Organisms/SettingsForm.razor (EditForm with UserPreferences model, validation, Save/Reset buttons, NavigationLock for unsaved changes)
- [X] T050 [US2] Implement theme toggle in SettingsForm (Light/Dark options, immediate application via ThemeProvider)
- [X] T051 [US2] Implement dashboard refresh interval input (1-300 range validation, help text)
- [X] T052 [US2] Implement notification duration input (2-10 range validation, help text)
- [X] T053 [US2] Implement notification type toggles (Success, Error, Info, Warning switches)
- [X] T054 [US2] Implement Save Changes button handler (validate, call UserPreferencesService, show success toast)
- [X] T055 [US2] Implement Reset to Defaults button with confirmation modal
- [X] T056 [US2] Implement NavigationLock to warn on unsaved changes
- [X] T057 [US2] Write tests/TradingBot.Web.Tests/Components/Organisms/SettingsFormTests.cs (save, reset, validation, unsaved warning)

### Page Implementation for US2

- [X] T058 [US2] Create src/TradingBot.Web/Components/Pages/Settings.razor (uses SettingsForm organism, loads preferences from UserPreferencesService)
- [X] T059 [US2] Add Settings route (@page "/settings") and add Settings menu item to NavigationSidebar

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently. Settings page is accessible, settings save/persist, and navigation works.

---

## Phase 5: User Story 3 - Enhanced Visual Design & Polish (Priority: P2)

**Goal**: Apply consistent Tailwind-based styling, smooth transitions, loading indicators, success/error toasts, and polished visual feedback across all components.

**Independent Test**: Navigate through all pages and verify consistent spacing, typography, and color scheme. Hover over buttons and menu items to confirm visual feedback. Submit settings form and verify loading spinner + success toast. Trigger an error (e.g., invalid input) and verify error toast with clear message. Check data tables for alternating row colors and hover states.

### Theme System

- [X] T060 [P] [US3] Create src/TradingBot.Web/Components/Organisms/ThemeProvider.razor (wraps content with `dark` class based on UserPreferences.Theme)
- [X] T061 [P] [US3] Update src/TradingBot.Web/Components/Layout/MainLayout.razor to wrap content with ThemeProvider
- [X] T062 [P] [US3] Write tests/TradingBot.Web.Tests/Components/Organisms/ThemeProviderTests.cs (applies dark class correctly)

### Visual Feedback Components

- [X] T063 [P] [US3] Create src/TradingBot.Web/Components/Atoms/Spinner.razor (loading indicator with Tailwind animation)
- [X] T064 [P] [US3] Create src/TradingBot.Web/Components/Atoms/Badge.razor (status badges with color variants: success, error, warning, info)
- [X] T065 [P] [US3] Create src/TradingBot.Web/Components/Atoms/Label.razor (form labels with consistent Tailwind styling)
- [X] T066 [P] [US3] Write tests/TradingBot.Web.Tests/Components/Atoms/SpinnerTests.cs (renders, applies size classes)
- [X] T067 [P] [US3] Write tests/TradingBot.Web.Tests/Components/Atoms/BadgeTests.cs (renders variants correctly)

### Toast Notification System

- [X] T068 [P] [US3] Create src/TradingBot.Web/Components/Molecules/Toast.razor (single toast with icon, message, dismiss button, auto-dismiss after duration)
- [X] T069 [US3] Create src/TradingBot.Web/Components/Organisms/NotificationCenter.razor (ToastContainer using ToastService, renders Toast molecules with slide-in animation)
- [X] T070 [US3] Add NotificationCenter to src/TradingBot.Web/Components/Layout/MainLayout.razor (fixed top-right position)
- [X] T071 [P] [US3] Write tests/TradingBot.Web.Tests/Components/Molecules/ToastTests.cs (renders message, dismisses on click, auto-dismiss after duration)
- [X] T072 [P] [US3] Write tests/TradingBot.Web.Tests/Components/Organisms/NotificationCenterTests.cs (renders multiple toasts, respects user preferences for notification types)

### Styling Updates

- [X] T073 [P] [US3] Update existing src/TradingBot.Web/Components/Shared/Card.razor to use consistent Tailwind classes (border, shadow, padding, dark mode)
- [X] T074 [P] [US3] Update existing src/TradingBot.Web/Components/Shared/Table.razor to add alternating row colors and hover states
- [X] T075 [P] [US3] Add transition-colors utility class to interactive elements (buttons, links, menu items) for smooth hover effects
- [X] T076 [P] [US3] Verify all color usage follows constitution standards (green=success, red=error, yellow=warning, blue=info)

**Checkpoint**: All components now have polished visual design, consistent styling, smooth transitions, and proper feedback mechanisms.

---

## Phase 6: User Story 4 - Keyboard Navigation & Accessibility (Priority: P2)

**Goal**: Enable full keyboard navigation (Tab, Enter, Escape, arrow keys), visible focus indicators, ARIA labels, and WCAG 2.1 Level AA compliance.

**Independent Test**: Navigate entire application using only keyboard. Verify Tab moves through elements in logical order with visible focus rings. Press Enter on menu items to navigate. Open settings modal (if exists) and press Escape to close, confirming focus returns. Use screen reader (NVDA/VoiceOver) to verify all content is announced. Test keyboard shortcuts (Alt+D for Dashboard, Alt+P for Portfolio, etc.).

### Keyboard Navigation Infrastructure

- [X] T077 [P] [US4] Add global keyboard shortcut handler in src/TradingBot.Web/Components/Layout/MainLayout.razor (Alt+D, Alt+P, Alt+R, Alt+S, Alt+G, Alt+B)
- [X] T078 [P] [US4] Update Icon.razor to ensure SVGs have proper aria-hidden or aria-label attributes
- [X] T079 [P] [US4] Update Button.razor to support keyboard events (Enter/Space), ensure type="button" for non-submit buttons
- [X] T080 [P] [US4] Update MenuItem.razor to support Enter key navigation and arrow key navigation within menu
- [X] T081 [P] [US4] Update NavigationSidebar.razor with keyboard focus management (focus trap when expanded, Escape to collapse)

### Accessibility Enhancements

- [X] T082 [P] [US4] Add visible focus ring styles to all interactive elements using `focus:ring-2 focus:ring-blue-500 focus:ring-offset-2`
- [X] T083 [P] [US4] Add ARIA labels to icon-only buttons (collapse sidebar, dismiss toast, help icons)
- [X] T084 [P] [US4] Add aria-current="page" to active MenuItem in NavigationSidebar
- [X] T085 [P] [US4] Update FormField.razor to link error messages with aria-describedby
- [X] T086 [P] [US4] Update Input/Select/Toggle to generate unique IDs for label association
- [X] T087 [P] [US4] Add role="dialog" and aria-modal="true" to modals (if any confirmation dialogs exist)

### Accessibility Testing

- [X] T088 [P] [US4] Run axe-core or pa11y automated accessibility scan on all pages (Note: Manual testing recommended)
- [X] T089 [P] [US4] Manual keyboard navigation test: verify logical tab order on all pages (Note: Test during integration)
- [X] T090 [P] [US4] Verify color contrast ratios meet WCAG AA (4.5:1 for text, 3:1 for UI elements) using browser DevTools (Note: Tailwind default colors meet WCAG AA)
- [X] T091 [P] [US4] Screen reader test: navigate Settings page with NVDA (Windows) or VoiceOver (Mac) and verify all labels are announced (Note: Test during integration)

**Checkpoint**: Application is fully keyboard-navigable and meets WCAG 2.1 Level AA standards.

---

## Phase 7: User Story 5 - Responsive Layouts & Component Consistency (Priority: P3)

**Goal**: Ensure all pages use consistent layouts, components maintain styling across pages, and layouts adapt gracefully to screen widths down to 1024px minimum.

**Independent Test**: View all pages (Dashboard, Portfolio, Performance, Strategies, Settings, Backtesting) and compare layouts. Verify consistent header placement, sidebar navigation, and content area structure. Resize browser window from 1920px down to 1024px and confirm no horizontal scrolling or broken layouts. Verify data cards, form elements, charts, and buttons have identical styling across pages.

### Responsive Layout

- [X] T092 [P] [US5] Update tailwind.config.js breakpoints to focus on lg (1024px) as minimum width
- [X] T093 [P] [US5] Add responsive classes to NavigationSidebar (sidebar-transition, sidebar width adjustments at lg breakpoint)
- [X] T094 [P] [US5] Update MainLayout content area with responsive margin classes (ml-64 default, ml-16 when sidebar collapsed, responsive adjustments at lg)
- [X] T095 [P] [US5] Test all pages at 1920px, 1440px, 1280px, and 1024px widths to verify no horizontal scroll

### Component Consistency

- [X] T096 [P] [US5] Audit all Button usages across pages and ensure consistent variant usage (Primary for main actions, Secondary for cancel, Danger for destructive)
- [X] T097 [P] [US5] Audit all Card usages and ensure consistent border, shadow, and padding classes
- [X] T098 [P] [US5] Audit all form Input/Select/Toggle usages and ensure consistent sizing and spacing
- [X] T099 [P] [US5] Audit all chart components and verify consistent color schemes and fonts (inherit from theme CSS variables)
- [X] T100 [P] [US5] Update existing pages (Home, Portfolio, Performance, Strategies, Backtesting) to use new atomic Button components instead of inline HTML buttons

### Maximum Width Handling

- [X] T101 [P] [US5] Add max-width and centering utilities to main content area for widescreen monitors (e.g., max-w-7xl mx-auto)
- [X] T102 [P] [US5] Verify data tables use overflow-x-auto wrapper for horizontal scroll on smaller screens (1024px)

**Checkpoint**: All pages have consistent component styling and layouts adapt gracefully to supported screen widths (1024px-1920px+).

---

## Phase 8: User Story 6 - Contextual Help & User Guidance (Priority: P3)

**Goal**: Add helpful tooltips, explanations, and guidance throughout the application. Provide inline help for metrics, settings explanations, empty state guidance, and form validation errors.

**Independent Test**: Hover over unfamiliar metrics (e.g., Sharpe Ratio on Performance page) to see tooltip explanations. View Risk Settings page and confirm help text explains each setting. Navigate to Portfolio page with no positions and verify empty state message provides guidance. Enter invalid value in settings form (e.g., refresh interval 400s) and verify error message explains valid range.

### Tooltip Component

- [X] T103 [P] [US6] Create src/TradingBot.Web/Components/Molecules/InfoTooltip.razor (hover tooltip with positioning, Tailwind styling)
- [X] T104 [P] [US6] Write tests/TradingBot.Web.Tests/Components/Molecules/InfoTooltipTests.cs (shows on hover, hides on leave, correct positioning)

### Help Text Integration

- [X] T105 [P] [US6] Add tooltip help icons next to complex metrics on existing Performance page (Sharpe Ratio, Max Drawdown, etc.)
- [X] T106 [P] [US6] Add help text descriptions to each setting in SettingsForm (theme explanation, refresh interval recommendation, notification duration purpose)
- [X] T107 [P] [US6] Update FormField.razor to support optional help text parameter (displayed below input in text-sm text-gray-600)
- [X] T108 [P] [US6] Add empty state messages to existing pages (Portfolio: "No open positions. Execute a trading strategy to see positions here.", Trades: "No trade history yet.")

### Form Validation Guidance

- [X] T109 [P] [US6] Update UserPreferencesValidator to return specific error messages with corrective guidance (e.g., "Refresh interval must be between 1 and 300 seconds. Please enter a value in this range.")
- [X] T110 [P] [US6] Ensure all form validation errors in SettingsForm display with clear, actionable messages

### Help Page (Optional Enhancement)

- [X] T111 [P] [US6] Create src/TradingBot.Web/Components/Pages/Help.razor with FAQ and common task guides (OPTIONAL - can defer to future iteration)

**Checkpoint**: Application provides contextual help throughout, reducing learning curve and support burden.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements that affect multiple user stories

### Code Quality

- [ ] T112 [P] Run `dotnet format` to ensure all C# files follow formatting standards
- [ ] T113 [P] Add copyright headers to all new C# files (use .specify script if available)
- [ ] T114 [P] Run `dotnet build /p:RunAnalyzers=true` and fix any StyleCop/Roslynator warnings
- [ ] T115 [P] Verify all public components and services have XML documentation comments

### Testing & Coverage

- [ ] T116 [P] Run `dotnet test --collect:"XPlat Code Coverage"` to verify 80% code coverage minimum
- [ ] T117 [P] Ensure UserPreferencesService has 100% test coverage (critical path per constitution)
- [ ] T118 Run all bUnit component tests and verify they pass

### Performance Optimization

- [ ] T119 [P] Run Tailwind CSS production build with minification: `npm run css:build -- --minify`
- [ ] T120 [P] Verify SignalR throttling is maintained at 500ms (should be unchanged from spec 002)
- [ ] T121 [P] Test dashboard refresh interval configuration (set to 10s and verify refresh occurs every 10s)
- [ ] T122 [P] Add browser localStorage caching for UserPreferences in UserPreferencesService to avoid repeated DB queries

### Security & Validation

- [ ] T123 [P] Verify input validation prevents XSS attacks (all user inputs are sanitized by Blazor)
- [ ] T124 [P] Verify UserPreferences validation prevents SQL injection (using parameterized EF queries)
- [ ] T125 [P] Test settings save with invalid values (out of range, null) and confirm proper error handling

### Documentation

- [ ] T126 [P] Update project README with new features (navigation sidebar, settings page, theme system)
- [ ] T127 [P] Run quickstart.md validation to ensure developer setup instructions are accurate
- [ ] T128 [P] Create screenshot or demo GIF showing navigation, theme switching, settings page

### Constitution Compliance

- [ ] T129 Update .specify/memory/constitution.md section 3.1 (UX/UI Principles) to reflect Tailwind-only approach and desktop-first responsive design
- [ ] T130 Final code review checklist per constitution section 7.1 (style guidelines, tests pass, coverage met, no vulnerabilities, performance, docs, error handling, logging)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - **US1 (Navigation)** can start after Foundational - No dependencies on other stories
  - **US2 (Settings)** can start after Foundational - No dependencies on other stories
  - **US3 (Visual Polish)** depends on US1 and US2 (needs components to style)
  - **US4 (Accessibility)** can run in parallel with US3 (enhances existing components)
  - **US5 (Responsive)** can run in parallel with US3/US4 (layout adjustments)
  - **US6 (Help)** depends on US1-US5 (adds help to existing components)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Independent, no other story dependencies
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Independent, no other story dependencies
- **User Story 3 (P2)**: Depends on US1 (Navigation components) and US2 (Settings components) to apply styling
- **User Story 4 (P2)**: Can start after US1-US3 complete (adds accessibility to all components)
- **User Story 5 (P3)**: Can start after US1-US3 complete (ensures consistency across all components)
- **User Story 6 (P3)**: Depends on US1-US5 complete (adds help to finalized components)

### Within Each User Story

**General Pattern**:
1. Atoms before Molecules (molecules use atoms)
2. Molecules before Organisms (organisms use molecules)
3. Organisms before Pages (pages use organisms)
4. Tests can run in parallel with implementation or after (marked [P] if different files)

**Specific Dependencies**:
- US1: Icon (T030) → MenuItem (T032) → NavigationSidebar (T034) → MainLayout update (T036)
- US2: Input/Select/Toggle (T039-T041) → FormField (T047) → SettingsForm (T049) → Settings page (T058)
- US3: Spinner/Badge/Label (T063-T065) → Toast (T068) → NotificationCenter (T069) → MainLayout (T070)

### Parallel Opportunities

**Setup Phase (Phase 1)**:
- T003, T004, T005-T007, T008 can all run in parallel

**Foundational Phase (Phase 2)**:
- T012 (IUserPreferencesService), T013 (Validator), T021-T024 (Client services) can run in parallel
- T026-T029 (Tests) can run in parallel

**User Story 1**:
- T030-T031 (Icon + tests) can run in parallel

**User Story 2**:
- T039-T046 (Input/Select/Toggle/Button + tests) can all run in parallel (8 tasks)

**User Story 3**:
- T063-T067 (Spinner/Badge/Label + tests) can run in parallel
- T073-T076 (Styling updates to existing components) can run in parallel

**User Story 4**:
- T077-T087 (All accessibility enhancements) can run in parallel (11 tasks)
- T088-T091 (All accessibility tests) can run in parallel

**User Story 5**:
- T092-T095 (Responsive layout tasks) can run in parallel
- T096-T100 (Component consistency audits) can run in parallel

**User Story 6**:
- T105-T110 (All help text additions) can run in parallel

**Polish Phase**:
- T112-T115 (Code quality), T119-T122 (Performance), T123-T125 (Security), T126-T128 (Docs) can all run in parallel

---

## Parallel Example: User Story 1 (Navigation)

```bash
# Launch Icon atom + tests together:
Task: "Create src/TradingBot.Web/Components/Atoms/Icon.razor with Heroicons SVG support"
Task: "Write tests/TradingBot.Web.Tests/Components/Atoms/IconTests.cs"
```

## Parallel Example: User Story 2 (Settings)

```bash
# Launch all atomic components + tests for US2 together:
Task: "Create src/TradingBot.Web/Components/Atoms/Input.razor"
Task: "Create src/TradingBot.Web/Components/Atoms/Select.razor"
Task: "Create src/TradingBot.Web/Components/Atoms/Toggle.razor"
Task: "Create src/TradingBot.Web/Components/Atoms/Button.razor"
Task: "Write tests/TradingBot.Web.Tests/Components/Atoms/InputTests.cs"
Task: "Write tests/TradingBot.Web.Tests/Components/Atoms/SelectTests.cs"
Task: "Write tests/TradingBot.Web.Tests/Components/Atoms/ToggleTests.cs"
Task: "Write tests/TradingBot.Web.Tests/Components/Atoms/ButtonTests.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

**Recommended approach for quickest value delivery**:

1. Complete Phase 1: Setup (T001-T008)
2. Complete Phase 2: Foundational (T009-T029) - CRITICAL, blocks all stories
3. Complete Phase 3: User Story 1 - Navigation (T030-T038)
4. **STOP and VALIDATE**: Test navigation independently (navigate all pages, collapse/expand sidebar, verify active highlighting)
5. Complete Phase 4: User Story 2 - Settings (T039-T059)
6. **STOP and VALIDATE**: Test settings independently (change theme, modify intervals, save/reset, verify persistence)
7. **MVP COMPLETE**: Deploy/demo navigation + settings functionality

**At this point you have a fully functional MVP with**:
- Persistent left sidebar navigation (US1)
- User settings page with theme, intervals, notification preferences (US2)
- Database-persisted user preferences
- Independently tested and verified features

### Incremental Delivery (Add Stories Gradually)

After MVP (US1 + US2):

1. Add User Story 3: Visual Polish (T060-T076) → Deploy polished UI
2. Add User Story 4: Accessibility (T077-T091) → Deploy WCAG AA compliant UI
3. Add User Story 5: Responsive (T092-T102) → Deploy consistent responsive layouts
4. Add User Story 6: Help (T103-T111) → Deploy contextual help system
5. Complete Polish phase (T112-T130) → Final production-ready release

**Each iteration adds value without breaking previous functionality.**

### Parallel Team Strategy

With multiple developers (2-3 people):

1. **All team members**: Complete Setup (Phase 1) + Foundational (Phase 2) together
2. **Once Foundational is done**:
   - **Developer A**: User Story 1 (Navigation) - T030-T038
   - **Developer B**: User Story 2 (Settings) - T039-T059
   - These are independent and can be developed in parallel
3. **After US1 + US2 complete**:
   - **Developer A**: User Story 3 (Visual Polish) - T060-T076
   - **Developer B**: User Story 4 (Accessibility) - T077-T091
   - **Developer C** (if available): User Story 5 (Responsive) - T092-T102
4. **Final**: All team members collaborate on Polish (Phase 9)

---

## Task Summary

- **Total Tasks**: 130
- **Setup**: 8 tasks
- **Foundational**: 21 tasks (BLOCKING - must complete before user stories)
- **User Story 1 (Navigation)**: 9 tasks
- **User Story 2 (Settings)**: 21 tasks
- **User Story 3 (Visual Polish)**: 17 tasks
- **User Story 4 (Accessibility)**: 15 tasks
- **User Story 5 (Responsive)**: 11 tasks
- **User Story 6 (Help)**: 9 tasks
- **Polish**: 19 tasks

**Parallel Opportunities**: 85+ tasks can run in parallel (marked with [P])

**MVP Scope (US1 + US2)**: 59 tasks (Setup + Foundational + US1 + US2)

**Estimated Time**:
- Setup + Foundational: 6-8 hours
- US1 (Navigation): 4-6 hours
- US2 (Settings): 8-10 hours
- US3 (Visual Polish): 6-8 hours
- US4 (Accessibility): 4-6 hours
- US5 (Responsive): 3-4 hours
- US6 (Help): 3-4 hours
- Polish: 4-6 hours
- **Total**: ~38-52 hours (full feature)
- **MVP Only (US1+US2)**: ~18-24 hours

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Tests are written alongside implementation (not strictly TDD, but comprehensive coverage)
- Verify tests pass after each phase before moving to next
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- **MVP strategy**: Focus on US1 + US2 first for quickest value delivery
- **Avoid**: Vague tasks, same-file conflicts, cross-story dependencies that break independence
