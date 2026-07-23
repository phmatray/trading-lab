# Feature Specification: Component Refactoring and Organization

**Feature Branch**: `004-component-refactor`
**Created**: 2025-01-08
**Status**: Draft
**Input**: User description: "Refactor the webproject without adding or removing features implemented. We need to logically organize files, add a prefix to our component (ex: TbButton, TbCard...), create new components (Atoms, Molecules and Organisms) enforce the usage of the components on all pages, avoid to have 2 _import files, remove the using not used."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Component Consistency Across Application (Priority: P1)

Developers working on the TradingBot.Web project need to find and use components consistently across all pages without encountering duplicate or conflicting implementations.

**Why this priority**: This is the foundation of the refactoring effort. Without consistent, predictable components, all other improvements will be undermined by confusion and maintenance issues.

**Independent Test**: Can be fully tested by verifying all pages use the same component implementations (e.g., all buttons use TbButton, no usage of duplicate Button components) and delivers immediate value through reduced confusion and bugs.

**Acceptance Scenarios**:

1. **Given** a developer needs to add a button to a page, **When** they search for button components, **Then** they find exactly one button component (TbButton) with clear documentation
2. **Given** multiple pages use card components, **When** inspecting the markup, **Then** all pages use the same TbCard component with consistent styling
3. **Given** a developer needs to understand available components, **When** they browse the Components folder, **Then** they see a clear hierarchy (Atoms/Molecules/Organisms) with Tb-prefixed components

---

### User Story 2 - Improved Developer Navigation and Discoverability (Priority: P2)

Developers need to quickly locate specific components and understand the component hierarchy without navigating through multiple folders or encountering organizational inconsistencies.

**Why this priority**: Efficient navigation directly impacts developer productivity and reduces onboarding time for new team members.

**Independent Test**: Can be tested by timing how long it takes a developer to locate specific components (e.g., "find the position card component") and by verifying all related files are co-located (component + supporting types).

**Acceptance Scenarios**:

1. **Given** a developer needs to find all basic UI elements, **When** they navigate to Components/Atoms, **Then** they find all fundamental components (buttons, inputs, labels) prefixed with Tb
2. **Given** a developer needs to modify a strategy-related component, **When** they navigate to Components/Features/Strategy, **Then** they find all strategy components in one location
3. **Given** a developer opens any page file, **When** they check the imports, **Then** they see a single consolidated import file with no unused namespaces
4. **Given** a developer needs to understand component composition, **When** they examine molecule components, **Then** they see clear usage of Tb-prefixed atoms within molecules

---

### User Story 3 - Reduced Code Duplication and Maintenance Burden (Priority: P2)

Development team needs to maintain a single source of truth for each UI component to prevent bugs from duplicate implementations and reduce maintenance overhead.

**Why this priority**: Eliminating duplicates prevents divergent implementations and ensures bug fixes and enhancements are applied consistently.

**Independent Test**: Can be tested by searching for duplicate component names and verifying all references point to the canonical Tb-prefixed implementation.

**Acceptance Scenarios**:

1. **Given** the legacy Shared/Button.razor exists, **When** the refactoring is complete, **Then** only TbButton exists and all page references use TbButton
2. **Given** supporting types (enums, helpers) are scattered across Models and Components, **When** reorganized, **Then** all supporting types are co-located with their components
3. **Given** two _Imports.razor files exist (Pages and Components), **When** consolidated, **Then** a single _Imports.razor at the root level imports all necessary namespaces

---

### User Story 4 - Clear Atomic Design Hierarchy (Priority: P3)

Developers need to understand which components are basic building blocks (Atoms), which are composite components (Molecules), and which are complex sections (Organisms) to make informed decisions about component reuse and composition.

**Why this priority**: While important for long-term architecture, this can be implemented after core consistency is achieved.

**Independent Test**: Can be tested by reviewing the component hierarchy and verifying no atoms depend on molecules, no molecules depend on organisms, and all components follow clear classification rules.

**Acceptance Scenarios**:

1. **Given** a developer creates a new composite component, **When** they reference the component hierarchy guide, **Then** they can clearly determine if it's a Molecule or Organism based on complexity
2. **Given** TbCard is used to compose other components, **When** examining its dependencies, **Then** it only depends on atoms (TbButton, TbIcon, etc.) confirming it's a molecule
3. **Given** feature-specific components exist (Dashboard, Portfolio), **When** categorized, **Then** they are properly classified as Organisms or moved to Features folder based on domain specificity

---

### Edge Cases

- What happens when a component is renamed but not all references are updated (broken references)?
- How does the system handle components that are borderline between Molecule and Organism classification?
- What happens if supporting enum types are referenced from multiple locations during refactoring?
- How are page-level components in Components/Pages folder handled vs Pages folder?
- What happens when consolidating _Imports.razor if namespace conflicts exist?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All UI components MUST be prefixed with "Tb" (e.g., TbButton, TbCard, TbModal) to clearly distinguish them as TradingBot components
- **FR-002**: Component organization MUST follow Atomic Design principles with three levels: Atoms (basic elements), Molecules (simple composites), and Organisms (complex sections)
- **FR-003**: Feature-specific components MUST be organized in a dedicated Features folder with subfolders by domain (Dashboard, Portfolio, Strategy, etc.)
- **FR-004**: All duplicate components MUST be eliminated with references updated to use the canonical Tb-prefixed version
- **FR-005**: Supporting types (enums, interfaces, helpers) MUST be co-located with their related components in the same folder
- **FR-006**: Pages MUST only exist in the /Pages folder; Components/Pages folder MUST be consolidated into /Pages
- **FR-007**: A single root-level _Imports.razor file MUST replace the two existing import files (Components/_Imports.razor and Pages/_Imports.razor)
- **FR-008**: All unused using statements MUST be removed from component files and the consolidated _Imports.razor
- **FR-009**: All existing pages MUST be updated to use the new Tb-prefixed components consistently
- **FR-010**: Component dependencies MUST respect the hierarchy: Atoms depend on nothing, Molecules depend only on Atoms, Organisms depend on Atoms and Molecules
- **FR-011**: All component files MUST maintain existing accessibility features (ARIA labels, keyboard navigation)
- **FR-012**: All component files MUST maintain existing styling approach (Tailwind CSS utility classes)
- **FR-013**: The refactoring MUST NOT add or remove any user-facing features, only reorganize and rename existing components
- **FR-014**: All component functionality MUST remain identical after refactoring (no behavioral changes)
- **FR-015**: File copyright headers MUST be preserved during refactoring
- **FR-016**: Component parameter names and types MUST remain unchanged to avoid breaking existing usage

### Key Entities *(include if feature involves data)*

- **Component**: A reusable UI element with a specific purpose, categorized as Atom, Molecule, or Organism
  - Name: Must be prefixed with "Tb"
  - Category: Atom, Molecule, or Organism
  - Dependencies: Other components it references
  - Location: File path within organized structure

- **Supporting Type**: Enums, interfaces, or helper classes that support component functionality
  - Related Component: The component it supports
  - Type: Enum, Interface, Helper class
  - Location: Co-located with component

- **Page**: A routable Blazor page that composes components
  - Route: URL path
  - Components Used: List of Tb-prefixed components referenced
  - Location: /Pages folder

- **Import Directive**: Namespace import statements
  - Scope: Global (in _Imports.razor)
  - Usage: Referenced by components/pages
  - Status: Used or Unused

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of components are prefixed with "Tb" with zero exceptions for custom components
- **SC-002**: Component hierarchy contains exactly three levels (Atoms, Molecules, Organisms) with clear classification criteria
- **SC-003**: Zero duplicate component implementations exist in the codebase
- **SC-004**: A single _Imports.razor file exists with zero unused namespace imports
- **SC-005**: 100% of pages successfully compile and render without errors after refactoring
- **SC-006**: All automated tests pass without modification (proving functionality is preserved)
- **SC-007**: Developer can locate any component by name in under 15 seconds using the new structure
- **SC-008**: Zero components violate the dependency hierarchy (Atoms → Molecules → Organisms)
- **SC-009**: 100% of feature-specific components are organized in Features subfolders
- **SC-010**: Code review checklist confirms zero functionality changes (pure refactoring)

## Assumptions *(mandatory)*

1. **Atomic Design Classification**: Components will be classified as:
   - **Atoms**: Single-purpose UI primitives (Button, Input, Icon, Badge, Label, Select, Spinner, Toggle)
   - **Molecules**: Compositions of 2-4 atoms serving a specific purpose (FormField, MenuItem, Toast, PageHeader, InfoTooltip, Card, Modal, Table)
   - **Organisms**: Complex sections combining molecules and atoms, often feature-specific (NavigationSidebar, NotificationCenter, SettingsForm, ThemeProvider, all feature-specific components)

2. **Prefix Application**: The "Tb" prefix will be applied to all custom components except:
   - Built-in Blazor components (Router, RouteView, etc.)
   - Third-party library components (ApexChart)
   - Layout components which may use full names (MainLayout, but TbNavMenu)

3. **Component Migration Priority**: Shared folder components will be migrated as follows:
   - Shared/Button.razor → **Delete** (duplicate of Atoms/Button.razor which becomes TbButton)
   - Shared/Card.razor → **Migrate** to Molecules/TbCard.razor
   - Shared/Modal.razor → **Migrate** to Molecules/TbModal.razor
   - Shared/Table.razor → **Migrate** to Molecules/TbTable.razor
   - Shared/ErrorBoundary.razor → **Migrate** to Organisms/TbErrorBoundary.razor
   - Shared/ToastContainer.razor → **Migrate** to Organisms/TbToastContainer.razor

4. **Feature Folder Organization**: Feature-specific components will be organized under Components/Features/ with subfolders:
   - Features/Dashboard/ (6 components)
   - Features/Portfolio/ (4 components)
   - Features/Strategy/ (2 components)
   - Features/Risk/ (2 components)
   - Features/Performance/ (2 components)
   - Features/Backtest/ (3 components)
   - Features/Charts/ (2 components)

5. **Supporting Types Co-location**: Enums and helper types currently in Models/ will be moved to reside with their components:
   - BadgeEnums.cs → Components/Atoms/TbBadge/ subfolder
   - SpinnerEnums.cs → Components/Atoms/TbSpinner/ subfolder
   - LabelSize.cs → Components/Atoms/TbLabel/ subfolder
   - IconName.cs, IconVariant.cs → Components/Atoms/TbIcon/ subfolder
   - TooltipPosition.cs → Components/Molecules/TbInfoTooltip/ subfolder

6. **Layout Components**: MainLayout and NavMenu will be handled as:
   - MainLayout.razor → Stays in Components/Layout/ (Blazor convention)
   - NavMenu.razor → **Consolidate** with NavigationSidebar.razor as TbNavigationSidebar in Organisms/ (appears to be duplicate functionality)

7. **Import Consolidation Strategy**: The consolidated _Imports.razor will be placed at:
   - `/Components/_Imports.razor` (root of Components folder)
   - Pages will inherit from Components/_Imports.razor automatically
   - Namespaces will include: Blazor core, TradingBot.Core, TradingBot.Web.Components.Atoms, TradingBot.Web.Components.Molecules, TradingBot.Web.Components.Organisms, TradingBot.Web.Services, TradingBot.Web.Models

8. **Pages Consolidation**: Components/Pages/ will be moved to /Pages:
   - Components/Pages/Settings.razor → Pages/Settings.razor
   - Components/Pages/Help.razor → Pages/Help.razor

9. **Testing Approach**: Existing functionality will be verified by:
   - Running all existing unit tests (must pass without changes)
   - Manual testing of each page to verify rendering
   - Visual regression testing if available
   - SignalR real-time updates must continue working

10. **Backward Compatibility**: Since this is an internal refactoring:
    - No API contracts are affected
    - No database schema changes
    - No configuration changes
    - Only internal component references change

## Out of Scope *(mandatory)*

- Adding new features or functionality to any component
- Removing existing features or functionality from any component
- Changing component behavior or styling (except consistent application of existing styles)
- Modifying component parameters or public APIs
- Refactoring Services, Models, or Hubs (only Components and Pages)
- Performance optimization or accessibility improvements beyond preserving existing implementations
- Updating Tailwind CSS configuration or adding new utility classes
- Modifying SignalR hub implementations
- Changing database schema or data access patterns
- Updating third-party dependencies or library versions
- Creating new automated tests (existing tests must pass as-is)
- Documentation updates (will be handled in a separate effort)

## Dependencies *(include if feature has external dependencies)*

### Internal Dependencies

- **Existing codebase**: All 51 components and 7 pages in current TradingBot.Web project
- **Build system**: npm build scripts for Tailwind CSS must continue working
- **Test suite**: All existing unit and integration tests must pass without modification

### External Dependencies

- **.NET 9 SDK**: Required for building and running the application
- **Blazor Server framework**: Component model and rendering engine
- **Tailwind CSS**: Styling framework (no changes to configuration)
- **Development tools**: IDE support for Razor component refactoring and find/replace operations

### Risk Dependencies

- **Code review process**: All changes must be reviewed to ensure no functionality changes
- **Version control**: Git branch management for the refactoring work
- **Testing environment**: Must be available to verify all pages render correctly

## Constraints *(include if there are limitations)*

### Technical Constraints

- **Zero downtime requirement**: Refactoring must be completed in a single deployable unit to avoid partial state
- **Test compatibility**: All existing tests must pass without modification (no test updates allowed)
- **Framework conventions**: Must respect Blazor Server conventions for component organization
- **File system limitations**: Component names must be valid file names (Tb prefix must not cause conflicts)

### Process Constraints

- **Pure refactoring**: Absolutely no feature additions or removals allowed
- **Atomic commit**: Entire refactoring should be completed in a single comprehensive commit or small atomic commits that maintain working state
- **Code freeze**: No parallel feature development on web components during refactoring to avoid merge conflicts

### Quality Constraints

- **StyleCop compliance**: All refactored files must pass StyleCop analyzer rules
- **Copyright headers**: All file headers must be preserved or updated with correct file names
- **Null safety**: C# nullable reference types must remain enabled and respected
- **Accessibility**: WCAG 2.1 Level AA compliance must be maintained

## Acceptance Criteria *(mandatory)*

### Definition of Done

The component refactoring is complete when:

1. **Component Naming**:
   - [ ] All custom components are prefixed with "Tb"
   - [ ] Zero duplicate component names exist
   - [ ] All component files use PascalCase naming (TbButton.razor)

2. **Component Organization**:
   - [ ] Components/Atoms/ contains all basic UI primitives
   - [ ] Components/Molecules/ contains all composite components
   - [ ] Components/Organisms/ contains all complex sections
   - [ ] Components/Features/ contains all domain-specific components in subfolders
   - [ ] Components/Layout/ contains MainLayout (Blazor convention)
   - [ ] Components/Shared/ folder is deleted

3. **Page Organization**:
   - [ ] All pages exist in /Pages folder only
   - [ ] Components/Pages/ folder is deleted
   - [ ] All page routes continue to work correctly

4. **Import Consolidation**:
   - [ ] Single _Imports.razor file exists in /Components
   - [ ] Pages/_Imports.razor is deleted
   - [ ] All namespaces are used (zero unused imports)
   - [ ] All components and pages compile successfully

5. **Supporting Types**:
   - [ ] All enums are co-located with their components
   - [ ] Models/ folder no longer contains component-specific enums
   - [ ] All enum references are updated to new locations

6. **Component Updates**:
   - [ ] All pages use Tb-prefixed components
   - [ ] Zero references to legacy non-prefixed components
   - [ ] All component functionality remains identical

7. **Quality Checks**:
   - [ ] All existing unit tests pass without modification
   - [ ] StyleCop analyzer shows zero warnings
   - [ ] dotnet build succeeds with zero errors
   - [ ] All pages render correctly in browser testing
   - [ ] SignalR real-time updates continue working

8. **Dependency Hierarchy**:
   - [ ] No Atom components reference Molecules or Organisms
   - [ ] No Molecule components reference Organisms
   - [ ] Feature components appropriately classified

9. **Code Quality**:
   - [ ] Copyright headers preserved with correct file names
   - [ ] Zero unused using statements
   - [ ] Consistent code formatting (Blazor conventions)

10. **Documentation**:
    - [ ] Component hierarchy documented in a README (if doesn't exist)
    - [ ] Classification guidelines for future components documented