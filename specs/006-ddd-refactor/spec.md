# Feature Specification: DDD Architecture Refactoring

**Feature Branch**: `006-ddd-refactor`
**Created**: 2025-01-15
**Status**: Draft
**Input**: User description: "remove CLI app, eliminate duplicate classes and implement ddd using Ardalis.SharedKernel (https://github.com/ardalis/Ardalis.SharedKernel/tree/main)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Simplified Application Entry Point (Priority: P1)

As a trader, I want to access all trading bot functionality through a single modern web interface, eliminating the need to switch between CLI and web applications or manage duplicate configurations.

**Why this priority**: The CLI application duplicates functionality already present in the web dashboard, creating maintenance overhead and potential inconsistencies. Removing it simplifies the codebase while improving user experience by providing a single, consistent interface.

**Independent Test**: Can be fully tested by verifying all CLI commands (strategy management, portfolio viewing, backtesting, risk configuration) are accessible through the web dashboard and deliver identical functionality.

**Acceptance Scenarios**:

1. **Given** a user previously accessed strategy management via CLI, **When** they open the web dashboard, **Then** they can perform all strategy operations (list, enable, disable, configure) through the web UI
2. **Given** a user needs to run a backtest, **When** they navigate to the backtest page, **Then** they can execute backtests with the same configuration options previously available in CLI
3. **Given** a user wants to view portfolio status, **When** they access the web dashboard, **Then** they see all portfolio information (positions, trades, performance) previously shown in CLI dashboard

---

### User Story 2 - Consistent Domain Model (Priority: P1)

As a developer maintaining the trading bot, I want domain entities defined once without duplication, so that business logic changes only require updates in a single location.

**Why this priority**: Duplicate classes (e.g., RiskSettings in both Models/Risk and Models/Configuration) create maintenance burden, increase bug risk, and violate DRY principle. A single source of truth for domain entities is fundamental to code quality.

**Independent Test**: Can be tested by searching the codebase for duplicate class names and verifying each domain concept has exactly one canonical implementation.

**Acceptance Scenarios**:

1. **Given** the system has duplicate RiskSettings classes, **When** duplicate elimination is complete, **Then** only one RiskSettings class exists with all required properties
2. **Given** developers need to modify a domain entity, **When** they search for the entity class, **Then** they find exactly one definition to update
3. **Given** the system persists domain data, **When** an entity is saved, **Then** it uses the canonical domain model definition without transformation layers

---

### User Story 3 - Domain-Driven Design Patterns (Priority: P2)

As a developer extending the trading bot, I want domain entities to follow DDD best practices using established base classes and patterns, so that I can implement business logic consistently with reduced boilerplate code.

**Why this priority**: Implementing DDD patterns provides clear structure for domain logic (entities, value objects, aggregates, domain events) and reduces code duplication through shared abstractions. This improves code maintainability and makes the codebase easier for new developers to understand.

**Independent Test**: Can be tested by verifying domain entities inherit from appropriate base classes (EntityBase, ValueObject) and follow DDD patterns (aggregate roots, domain events) with consistent implementation across the domain layer.

**Acceptance Scenarios**:

1. **Given** a new trading entity needs to be created, **When** a developer implements it, **Then** they can extend EntityBase or ValueObject with standardized ID generation and equality comparison
2. **Given** business logic emits a domain event (e.g., OrderFilled), **When** the event is raised, **Then** it inherits from DomainEventBase and is dispatched through the domain event mechanism
3. **Given** domain entities reference related entities, **When** developers model relationships, **Then** they follow aggregate root patterns to maintain consistency boundaries
4. **Given** developers need repository access, **When** they implement data access, **Then** they use IRepository and IReadRepository interfaces with consistent patterns

---

### Edge Cases

- What happens when CLI-specific configuration exists but web application doesn't have equivalent settings?
  - Migration path should preserve all settings, mapping CLI config to web app user preferences

- How does the system handle existing data created by CLI application?
  - Database schema remains compatible; all data is accessible through web interface without migration

- What if duplicate classes have conflicting property definitions or different validation logic?
  - Canonical version should incorporate all validations and properties from all duplicates; document any breaking changes

- How are CLI-specific dependencies (Spectre.Console) removed without breaking other projects?
  - Remove TradingBot.Cli project entirely; update solution file and remove all CLI project references

## Requirements *(mandatory)*

### Functional Requirements

#### CLI Removal
- **FR-001**: System MUST remove the TradingBot.Cli project completely from the solution
- **FR-002**: All CLI command functionality (strategy management, portfolio operations, backtesting, risk configuration) MUST be verified as available through web dashboard
- **FR-003**: System MUST update database migrations to use TradingBot.Web as startup project instead of TradingBot.Cli
- **FR-004**: Documentation MUST be updated to remove all CLI-related instructions and examples

#### Duplicate Class Elimination
- **FR-005**: System MUST consolidate duplicate RiskSettings classes into a single canonical implementation in Core layer
- **FR-006**: System MUST consolidate duplicate EquityPoint classes into a single implementation
- **FR-007**: System MUST identify and merge any other duplicate entity or value object classes
- **FR-008**: All references to eliminated duplicate classes MUST be updated to use the canonical version
- **FR-009**: Database entity configurations MUST reference the canonical class definitions

#### DDD Implementation with Ardalis.SharedKernel
- **FR-010**: System MUST add Ardalis.SharedKernel NuGet package to TradingBot.Core project
- **FR-011**: Domain entities representing aggregate roots (Order, Position, Account) MUST implement IAggregateRoot
- **FR-012**: All domain entities MUST extend EntityBase with appropriate ID type (Guid or long)
- **FR-013**: Value objects (e.g., SymbolInfo, RiskParameters) MUST extend ValueObject base class
- **FR-014**: Domain events (OrderFilled, PositionClosed, SignalGenerated) MUST extend DomainEventBase
- **FR-015**: Repository interfaces MUST extend IRepository<T> and IReadRepository<T> from SharedKernel
- **FR-016**: System MUST implement IDomainEventDispatcher for handling domain events
- **FR-017**: Entities that emit domain events MUST implement IHasDomainEvents interface

### Key Entities

- **EntityBase**: Base class for all domain entities with identity (Order, Position, Trade, Account)
  - Provides ID property, equality comparison, and domain event support

- **IAggregateRoot**: Marker interface identifying aggregate roots
  - Applied to entities that form consistency boundaries (Order, Position, Account)

- **ValueObject**: Base class for objects defined by their attributes rather than identity
  - Used for RiskParameters, SymbolInfo, configuration objects

- **DomainEventBase**: Base class for all domain events
  - OrderFilled, PositionClosed, SignalGenerated extend this

- **IRepository**: Generic repository interface for aggregate roots
  - Provides Add, Update, Delete, GetByIdAsync operations

- **IReadRepository**: Generic read-only repository interface
  - Provides query operations without modification capabilities

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Solution contains zero CLI-related projects (TradingBot.Cli and TradingBot.Cli.Tests removed)
- **SC-002**: Codebase has zero duplicate class definitions (verified by searching for duplicate type names)
- **SC-003**: All domain entities extend either EntityBase, ValueObject, or implement IAggregateRoot
- **SC-004**: All existing unit tests pass without modification to test logic (only project references updated)
- **SC-005**: Database migrations execute successfully with TradingBot.Web as startup project
- **SC-006**: Code coverage remains at or above current levels (80% minimum, 100% for critical paths)
- **SC-007**: Build completes with zero warnings and zero errors using all code analyzers
- **SC-008**: All domain events inherit from DomainEventBase and are dispatched through domain event infrastructure
- **SC-009**: Repository implementations use IRepository<T> and IReadRepository<T> interfaces consistently

## Assumptions

- **A-001**: All CLI functionality has equivalent implementation in web dashboard (verified in feature 005-web-app-functionality)
- **A-002**: No users are actively relying on CLI application as their primary interface
- **A-003**: Existing database schema is compatible with DDD entity implementations (no breaking migration required)
- **A-004**: Ardalis.SharedKernel base classes are compatible with current Entity Framework Core 10 usage
- **A-005**: Domain event infrastructure can integrate with existing SignalR hub for real-time updates
- **A-006**: SmartEnum pattern usage is compatible with DDD value object patterns
- **A-007**: Current repository pattern implementations can be adapted to IRepository<T> interface without data access logic changes

## Out of Scope

- **OOS-001**: Implementing event sourcing or CQRS patterns (beyond basic domain events)
- **OOS-002**: Migrating existing database data to different schema
- **OOS-003**: Creating new domain features or changing business logic
- **OOS-004**: Performance optimization or architectural changes beyond DDD pattern adoption
- **OOS-005**: Changing the web application UI or adding new user-facing features
- **OOS-006**: Implementing mediator pattern or replacing existing service layer
