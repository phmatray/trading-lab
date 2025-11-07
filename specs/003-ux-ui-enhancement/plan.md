# Implementation Plan: UX/UI Enhancement - Navigation & Settings

**Branch**: `003-ux-ui-enhancement` | **Date**: 2025-11-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-ux-ui-enhancement/spec.md`

## Summary

Implement comprehensive UX/UI enhancements for the TradingBot Blazor web application, including:
- **Persistent left sidebar navigation** with collapsible icon-only mode
- **User settings page** for theme (light/dark), refresh intervals, and notification preferences
- **Custom Tailwind-based component library** following Atomic Design pattern (atoms, molecules, organisms)
- **Heroicons integration** for consistent iconography
- **Theme system** using Tailwind dark mode with CSS variables
- **Toast notification system** with user-configurable display settings
- **Keyboard navigation and accessibility** compliance (WCAG 2.1 Level AA)

**Technical Approach** (from research.md):
- Build reusable atomic components (Button, Input, Icon, etc.) with Tailwind CSS
- No third-party component libraries (DaisyUI removed per user request)
- Hybrid state management (client UIState service + database-persisted preferences)
- SQLite database extension for UserPreferences table
- Single-user system (UserId = "default", no authentication changes)

---

## Technical Context

**Language/Version**: C# 12 / .NET 9
**Primary Dependencies**:
  - ASP.NET Core Blazor Server 9.0
  - Tailwind CSS 3.x (utility-first styling)
  - Heroicons (SVG icons)
  - Entity Framework Core 9 (SQLite)
  - SignalR (existing, for real-time updates)
  - Blazor-ApexCharts (existing, for data visualization)

**Storage**: SQLite via EF Core 9 (extend with UserPreferences table)
**Testing**: xUnit, bUnit (Blazor component testing), FakeItEasy (mocking), Shouldly (assertions)
**Target Platform**: Modern web browsers (Chrome, Edge, Firefox, Safari - last 2 versions), desktop-first (minimum 1024px width)
**Project Type**: Web (Blazor Server application)
**Performance Goals**:
  - API endpoints: < 200ms p95 (per constitution)
  - Page load: < 1.5s FCP, < 3.5s TTI
  - SignalR updates: Throttled to 500ms (2 updates/sec)
  - Smooth UI transitions: < 300ms

**Constraints**:
  - Desktop-only responsive design (1024px minimum width)
  - Single-user system (no multi-user authentication)
  - Must maintain existing Blazor Server architecture from spec 002
  - No mobile/touch optimization required

**Scale/Scope**:
  - Single web application
  - ~15-20 new reusable components (atoms + molecules + organisms)
  - 1 new database table (UserPreferences)
  - 1 new page (Settings)
  - Update existing MainLayout and navigation

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Code Quality Compliance

| Principle | Status | Notes |
|-----------|--------|-------|
| Clean Code Standards | ✅ PASS | Component-based architecture, descriptive naming, single responsibility |
| C# Coding Conventions | ✅ PASS | PascalCase, async/await, nullable reference types, IDisposable |
| SmartEnum Pattern | ✅ PASS | `Theme` will use SmartEnum (Light, Dark) |
| XML Documentation | ✅ PASS | All public components and services will have XML docs |
| File Headers | ✅ PASS | Copyright headers required per project standards |
| SOLID Principles | ✅ PASS | Interface-based services (IUserPreferencesService, IUserPreferencesRepository) |

### ✅ Testing Standards Compliance

| Requirement | Status | Notes |
|-------------|--------|-------|
| 80% Code Coverage | ✅ PASS | Will cover all services, repositories, and critical components |
| 100% Critical Paths | ✅ PASS | UserPreferences save/load is critical, will have 100% coverage |
| AAA Test Pattern | ✅ PASS | All tests follow Arrange-Act-Assert structure |
| bUnit for Components | ✅ PASS | Blazor component tests using bUnit framework |
| Test Independence | ✅ PASS | Each test uses isolated context/mocks |

### ⚠️ UX Consistency (Constitution Updated Required)

| Principle | Status | Notes |
|-----------|--------|-------|
| Consistent Design | ⚠️ UPDATE NEEDED | Constitution references DaisyUI - UPDATE to Tailwind-only approach |
| Accessibility (WCAG 2.1 AA) | ✅ PASS | Full keyboard navigation, ARIA labels, color contrast compliance |
| Responsive Design | ⚠️ PARTIAL | Constitution says "mobile-first" but spec is "desktop-first (1024px min)" |
| Error Handling | ✅ PASS | User-friendly messages, toast notifications, actionable feedback |
| Localization | ❌ OUT OF SCOPE | Multi-language support explicitly out of scope per spec |

**Action Required**: Update constitution section 3.1 to reflect:
- Tailwind CSS-only approach (no DaisyUI)
- Desktop-first responsive design for trading applications (not mobile-first)

### ✅ Performance Requirements Compliance

| Target | Status | Notes |
|--------|--------|-------|
| API < 200ms p95 | ✅ PASS | Simple CRUD operations on UserPreferences, < 10ms database query |
| Page Load < 1.5s FCP | ✅ PASS | Static assets, lazy load charts, Tailwind minified |
| Real-time Updates | ✅ PASS | SignalR throttling at 500ms already implemented (spec 002) |
| Caching Strategy | ✅ PASS | localStorage cache for preferences, 60s server cache |

### ✅ Security Standards Compliance

| Requirement | Status | Notes |
|-------------|--------|-------|
| Input Validation | ✅ PASS | Range validation on intervals (1-300s, 2-10s), validator class |
| Encryption at Rest | ✅ PASS | No sensitive data in preferences (theme, intervals) |
| No Secrets in Code | ✅ PASS | No API keys or credentials in preferences |
| Audit Trail | ⚠️ PARTIAL | CreatedAt/UpdatedAt timestamps, but no audit log for preference changes |

**Note**: Audit trail for financial transactions exists (spec 001), but preference changes are not financial.

### ✅ DevOps/CI-CD Compliance

| Requirement | Status | Notes |
|-------------|--------|-------|
| Git Workflow | ✅ PASS | Feature branch `003-ux-ui-enhancement`, PR required for merge |
| Automated Tests | ✅ PASS | CI runs full test suite on PR |
| Static Analysis | ✅ PASS | StyleCop, Roslynator enforced in build |
| Code Review | ✅ PASS | Will follow 7.1 review checklist |

### Gate Decision: ✅ **PASS WITH NOTES**

**Justification**:
- All critical standards met
- Constitution update needed (UX section) to reflect Tailwind-only + desktop-first approach
- No violations that block implementation
- Audit trail for preferences is non-critical (not financial data)

**Re-check after Phase 1 Design**: Verify component architecture aligns with clean code principles.

---

## Project Structure

### Documentation (this feature)

```text
specs/003-ux-ui-enhancement/
├── spec.md                  # Feature specification (user scenarios, requirements)
├── plan.md                  # This file (/speckit.plan output)
├── research.md              # Phase 0 output - technical decisions
├── data-model.md            # Phase 1 output - entities, validation, DB schema
├── quickstart.md            # Phase 1 output - developer setup guide
├── contracts/               # Phase 1 output - API contracts
│   └── user-preferences-api.yaml   # OpenAPI spec for preferences endpoints
└── tasks.md                 # Phase 2 output (/speckit.tasks - NOT created yet)
```

### Source Code (repository root)

**Blazor Server Web Application Structure**:

```text
src/TradingBot.Web/                         # Blazor Server project
├── Components/
│   ├── Atoms/                              # NEW: Basic reusable components
│   │   ├── Button.razor
│   │   ├── Input.razor
│   │   ├── Select.razor
│   │   ├── Toggle.razor
│   │   ├── Icon.razor                      # Heroicons SVG wrapper
│   │   ├── Badge.razor
│   │   ├── Spinner.razor
│   │   └── Label.razor
│   ├── Molecules/                          # NEW: Composite components
│   │   ├── FormField.razor                 # Label + Input + Error
│   │   ├── MenuItem.razor                  # Icon + Label navigation item
│   │   ├── Toast.razor                     # Single toast notification
│   │   ├── Tooltip.razor                   # Hover tooltip
│   │   └── Card.razor                      # Already exists, may update styling
│   ├── Organisms/                          # NEW: Complex feature components
│   │   ├── NavigationSidebar.razor         # Left sidebar navigation (replaces NavMenu)
│   │   ├── SettingsForm.razor              # Settings page form
│   │   ├── ThemeProvider.razor             # Theme context wrapper
│   │   └── NotificationCenter.razor        # Toast container manager
│   ├── Layout/
│   │   ├── MainLayout.razor                # UPDATE: Add ThemeProvider, NavigationSidebar
│   │   └── NavMenu.razor                   # DEPRECATED: Replace with NavigationSidebar
│   ├── Pages/
│   │   ├── Settings.razor                  # NEW: User settings page
│   │   ├── Home.razor                      # UPDATE: May update styling
│   │   ├── Portfolio.razor
│   │   ├── Performance.razor
│   │   ├── Strategies.razor
│   │   └── Backtest.razor
│   └── Shared/
│       ├── ToastContainer.razor            # UPDATE: Use new Toast molecule
│       └── ErrorBoundary.razor             # Already exists
├── Services/                               # NEW: Client-side services
│   ├── UIStateService.cs                   # Transient UI state (sidebar collapsed)
│   └── NavigationService.cs                # Navigation helpers, active route detection
├── Models/                                 # View models
│   └── ToastNotification.cs                # UPDATE: From data-model.md
├── Styles/
│   └── app.css                             # UPDATE: Add CSS variables for theme
├── wwwroot/
│   ├── css/
│   │   └── app.css                         # Generated Tailwind CSS (build output)
│   └── icons/                              # NEW: Optional Heroicons SVG storage
├── tailwind.config.js                      # UPDATE: Add dark mode, theme colors
├── package.json                            # Already exists (Tailwind build scripts)
└── Program.cs                              # UPDATE: Register UIStateService

src/TradingBot.Core/                        # Domain layer
├── Entities/
│   └── UserPreferences.cs                  # NEW: From data-model.md
├── ValueObjects/
│   └── Theme.cs                            # NEW: SmartEnum (Light, Dark)
├── Interfaces/
│   ├── IUserPreferencesRepository.cs       # NEW: Data access interface
│   └── IUserPreferencesService.cs          # NEW: Business logic interface
└── Validators/
    └── UserPreferencesValidator.cs         # NEW: Validate settings before save

src/TradingBot.Infrastructure/              # Data access layer
├── Persistence/
│   ├── Configurations/
│   │   └── UserPreferencesConfiguration.cs # NEW: EF Core fluent config
│   ├── Repositories/
│   │   └── UserPreferencesRepository.cs    # NEW: Repository implementation
│   └── Migrations/
│       └── [TIMESTAMP]_AddUserPreferences.cs  # NEW: EF migration
└── Services/
    └── UserPreferencesService.cs           # NEW: Business logic implementation

tests/TradingBot.Web.Tests/                 # NEW: Blazor component tests
├── Components/
│   ├── Atoms/
│   │   ├── ButtonTests.cs
│   │   ├── InputTests.cs
│   │   └── IconTests.cs
│   ├── Molecules/
│   │   └── FormFieldTests.cs
│   └── Organisms/
│       ├── NavigationSidebarTests.cs
│       └── SettingsFormTests.cs
└── Services/
    └── UIStateServiceTests.cs

tests/TradingBot.Core.Tests/                # Existing, add new tests
└── Validators/
    └── UserPreferencesValidatorTests.cs    # NEW

tests/TradingBot.Infrastructure.Tests/      # Existing, add new tests
├── Repositories/
│   └── UserPreferencesRepositoryTests.cs   # NEW
└── Services/
    └── UserPreferencesServiceTests.cs      # NEW
```

**Structure Decision**:
- **Web Application**: Single Blazor Server project with existing architecture from spec 002
- **Atomic Design**: Components organized by complexity (Atoms → Molecules → Organisms)
- **Clean Architecture**: Core (domain) → Infrastructure (data) → Web (presentation)
- **Test Mirroring**: Test structure mirrors source structure for discoverability

**Rationale**:
- Atomic Design provides clear component hierarchy and promotes reusability
- Separation of concerns: UI components (Web) vs domain logic (Core) vs data access (Infrastructure)
- Existing Blazor Server architecture maintained (no breaking changes)

---

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations requiring justification.** All complexity is justified by feature requirements:

| Potential Concern | Justification | Alternative Rejected |
|-------------------|---------------|----------------------|
| Three layers (Core/Infrastructure/Web) | Existing architecture from spec 002, maintains separation of concerns | Flat structure would violate clean architecture principles |
| Atomic Design (3 levels) | Industry-standard pattern for component libraries, promotes reusability | Flat component folder would be hard to navigate with 15-20 components |
| UserPreferences entity | Required to persist user settings per spec | localStorage-only rejected - can't sync across devices/browsers |
| SmartEnum for Theme | Project standard (all enums use SmartEnum per CLAUDE.md) | Regular enum rejected - violates project conventions |

---

## Phase 0: Research Summary

**Status**: ✅ Complete (see [research.md](./research.md))

**Key Decisions Made**:

1. **Component Architecture**: Atomic Design pattern with Tailwind CSS utility classes
2. **Icon System**: Heroicons via SVG integration (Icon.razor component)
3. **State Management**: Hybrid client/server (UIStateService for UI, UserPreferencesService for persistence)
4. **Theme Implementation**: Tailwind dark mode with CSS variables, class-based toggling
5. **Accessibility**: WCAG 2.1 Level AA via native HTML semantics + ARIA attributes
6. **Form Validation**: Data Annotations with custom UserPreferencesValidator
7. **Responsive Design**: Desktop-first (1024px min), collapsible sidebar for space optimization
8. **Performance**: SignalR throttling, lazy loading, localStorage caching
9. **Database Schema**: UserPreferences table with Theme SmartEnum, validation constraints
10. **Component Reusability**: Headless component pattern with RenderFragment slots

**All NEEDS CLARIFICATION items resolved.**

---

## Phase 1: Design Artifacts

**Status**: ✅ Complete

### 1. Data Model (data-model.md)

**Entities**:
- `UserPreferences` (database table)
  - Id, UserId, Theme, DashboardRefreshInterval, NotificationDuration
  - Notification toggles (Success, Error, Info, Warning)
  - CustomSettings (JSON for future extensibility)
  - CreatedAt, UpdatedAt timestamps
- `Theme` (SmartEnum: Light, Dark)

**Client-Side Models**:
- `UIStateService` (sidebar collapsed state)
- `ToastNotification` (toast message model)
- `ToastService` (notification manager)

**Validation**:
- DashboardRefreshInterval: 1-300 seconds
- NotificationDuration: 2-10 seconds
- Theme: Required, must be Light or Dark

**Repository/Service Interfaces**:
- `IUserPreferencesRepository`: Data access
- `IUserPreferencesService`: Business logic

### 2. API Contracts (contracts/user-preferences-api.yaml)

**Endpoints**:
- `GET /api/preferences`: Get user preferences
- `PUT /api/preferences`: Update preferences (with validation)
- `POST /api/preferences/reset`: Reset to defaults

**Note**: API is for internal Blazor Server communication, not external clients.

### 3. Quickstart Guide (quickstart.md)

**Developer Setup**:
- Prerequisites (tools, packages)
- Tailwind CSS configuration for dark mode
- Database migration commands
- Dependency injection setup
- Component development patterns (Atomic Design)
- Heroicons integration
- Testing with bUnit
- Code quality compliance

---

## Phase 2: Task Generation

**Status**: ⏳ Pending

**Next Command**: Run `/speckit.tasks` to generate dependency-ordered implementation tasks.

**Expected Task Categories**:
1. Database & Entities (Core layer)
2. Repository & Services (Infrastructure layer)
3. Atomic Components (Web layer - Atoms)
4. Composite Components (Web layer - Molecules)
5. Feature Components (Web layer - Organisms)
6. Settings Page & Navigation (Web layer - Pages/Layout)
7. Theme System Integration
8. Testing (Unit + Component tests)
9. Documentation & Polish

---

## Implementation Notes

### Critical Path Items

1. **Database Migration First**: Must create UserPreferences table before any service work
2. **Theme SmartEnum Early**: Required by UserPreferences entity
3. **Icon Component Before Navigation**: NavigationSidebar depends on Icon atom
4. **UIStateService Before Sidebar**: Sidebar depends on collapse state
5. **UserPreferencesService Before Settings Page**: Settings form needs service to save

### Dependency Order

```
1. Database/Entities → 2. Services → 3. Atoms → 4. Molecules → 5. Organisms → 6. Pages
```

### Testing Strategy

- **Unit Tests**: Services, validators, repositories (80% coverage minimum)
- **Component Tests**: All atoms, molecules, organisms using bUnit
- **Integration Tests**: Database migrations, UserPreferences CRUD
- **Manual Testing**: Keyboard navigation, screen reader compatibility, theme switching

### Performance Considerations

- **Lazy Load Charts**: Don't load ApexCharts until needed (already implemented in spec 002)
- **Throttle SignalR**: Existing 500ms throttle maintained
- **Minify Tailwind**: Production build uses `--minify` flag
- **localStorage Cache**: Preferences cached client-side to avoid repeated DB queries

### Accessibility Checklist

- [ ] All interactive elements have visible focus rings
- [ ] Keyboard shortcuts don't conflict with browser defaults
- [ ] ARIA labels on all icon-only buttons
- [ ] Color contrast meets WCAG AA (4.5:1 for text, 3:1 for UI elements)
- [ ] Modal focus trapping implemented
- [ ] Form error messages linked via aria-describedby

---

## Re-evaluation: Constitution Check (Post-Design)

**Status**: ✅ Still PASS

**Changes since initial check**: None that affect compliance

**Confirmed**:
- All components follow single responsibility principle
- Clean separation between presentation (Web), business logic (Core), and data access (Infrastructure)
- SmartEnum pattern maintained
- Test coverage plan meets 80% minimum (100% for UserPreferencesService critical path)
- Accessibility compliance built into all component designs

**Action Item**: Update constitution section 3.1 (UX/UI Principles) to reflect Tailwind-only approach and desktop-first responsive design. This is a documentation update, not a compliance violation.

---

## Summary & Next Steps

### Deliverables from `/speckit.plan`

- ✅ `plan.md` (this file)
- ✅ `research.md` (Phase 0)
- ✅ `data-model.md` (Phase 1)
- ✅ `contracts/user-preferences-api.yaml` (Phase 1)
- ✅ `quickstart.md` (Phase 1)

### Ready for Implementation

**Branch**: `003-ux-ui-enhancement`

**Planning Complete**: All design artifacts generated, technical unknowns resolved.

**Next Command**: `/speckit.tasks` to generate actionable task list with:
- Dependency-ordered tasks (DB → Services → Components → Pages)
- Acceptance criteria from spec.md
- Implementation file paths
- Testing requirements

### Key Takeaways

1. **No Component Library**: Build everything with Tailwind utilities (per user request)
2. **Atomic Design**: Clear component hierarchy for maintainability
3. **Single User**: UserId = "default", no auth changes needed
4. **Desktop-First**: 1024px minimum width, no mobile optimization
5. **Accessibility**: WCAG 2.1 AA compliance throughout
6. **Theme System**: Tailwind dark mode with CSS variables for smooth transitions

---

**Planning Status**: ✅ **COMPLETE**

**Estimated Implementation Time**:
- Core (DB/Entities/Services): 4-6 hours
- Components (Atoms/Molecules): 8-10 hours
- Organisms & Pages: 6-8 hours
- Testing: 6-8 hours
- **Total**: ~24-32 hours

**Ready to Execute**: Proceed with `/speckit.tasks` to begin implementation.
