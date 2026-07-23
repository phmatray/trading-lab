# Contributing to TradingBot

Thank you for your interest in contributing to TradingBot! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct (see CODE_OF_CONDUCT.md).

## Getting Started

1. **Fork the repository** and clone your fork
2. **Create a feature branch**: `git checkout -b feature/your-feature-name`
3. **Set up your development environment**:
   ```bash
   dotnet restore
   dotnet build
   ```

## Development Workflow

### Before You Start

1. Check existing issues and PRs to avoid duplicate work
2. For major changes, open an issue first to discuss the approach
3. Ensure you have the latest changes from `main`

### Making Changes

1. **Write clean, maintainable code**:
   - Follow the existing code style and conventions
   - Use meaningful variable and method names
   - Keep methods focused and concise
   - Add XML documentation comments for public APIs

2. **Follow C# coding standards**:
   - The project uses StyleCop analyzers for code quality
   - Run `dotnet build` to check for analyzer warnings
   - Fix all warnings before submitting

3. **Write tests**:
   - Add unit tests for all new functionality
   - Maintain or improve code coverage (target: 80%+)
   - Use xUnit, Shouldly, and FakeItEasy for testing
   - Run tests: `dotnet test`

4. **Check test coverage**:
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
   ```

### Commit Guidelines

- Use clear, descriptive commit messages
- Follow conventional commit format:
  - `feat:` for new features
  - `fix:` for bug fixes
  - `docs:` for documentation changes
  - `test:` for test additions/changes
  - `refactor:` for code refactoring
  - `chore:` for maintenance tasks

Example:
```
feat: add RSI indicator to strategy library

- Implement RSI calculation with configurable period
- Add unit tests covering edge cases
- Update documentation with usage examples
```

### Submitting a Pull Request

1. **Update your branch** with the latest `main`:
   ```bash
   git fetch origin
   git rebase origin/main
   ```

2. **Run the full test suite**:
   ```bash
   dotnet build
   dotnet test
   ```

3. **Push your changes**:
   ```bash
   git push origin feature/your-feature-name
   ```

4. **Create a Pull Request** with:
   - Clear title describing the change
   - Description explaining what, why, and how
   - Reference to related issues (e.g., "Closes #123")
   - Screenshots for UI changes
   - Test results and coverage impact

5. **Address review feedback**:
   - Respond to comments promptly
   - Make requested changes
   - Push updates to the same branch

## Project Structure

```
TradingBot/
├── src/
│   ├── TradingBot.Core/          # Domain models, interfaces
│   ├── TradingBot.Infrastructure/ # Data access, external services
│   ├── TradingBot.Engine/         # Trading engine, execution
│   ├── TradingBot.Strategies/     # Trading strategies, indicators
│   ├── TradingBot.Analytics/      # Performance analytics
│   └── TradingBot.Cli/            # Command-line interface
├── tests/                         # Unit and integration tests
├── docs/                          # Documentation
└── specs/                         # Feature specifications
```

## Areas for Contribution

### High Priority
- **Unit Tests**: Improve test coverage for existing features
- **Trading Strategies**: Implement new strategy types
- **Technical Indicators**: Add more indicators (VWAP, Ichimoku, etc.)
- **Risk Management**: Enhance position sizing and stop-loss features
- **Documentation**: Improve guides, examples, and API docs

### Medium Priority
- **CLI Commands**: Add new interactive commands
- **Data Sources**: Support additional market data providers
- **Performance**: Optimize backtesting engine performance
- **Analytics**: Add more performance metrics and visualizations

### Good First Issues
Look for issues labeled `good-first-issue` - these are beginner-friendly tasks that help you get familiar with the codebase.

## Testing Guidelines

### Unit Tests
- Test one thing at a time
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Use Arrange-Act-Assert pattern
- Mock external dependencies with FakeItEasy
- Use Shouldly for assertions

Example:
```csharp
[Fact]
public void CalculateSMA_WithValidData_ShouldReturnCorrectAverage()
{
    // Arrange
    var candles = CreateTestCandles(10);
    var period = 5;

    // Act
    var sma = IndicatorLibrary.CalculateSMA(candles, period);

    // Assert
    sma.ShouldBe(107m);
}
```

### Integration Tests
- Test complete workflows
- Use realistic data scenarios
- Verify system behavior end-to-end

## Style Guide

### C# Conventions
- Use PascalCase for public members
- Use camelCase for parameters and local variables
- Use `var` for local variables when type is obvious
- Prefer `string` over `String`, `int` over `Int32`
- Use expression bodies for single-line methods
- Add XML documentation for public APIs

### Documentation
- Use clear, concise language
- Include code examples where helpful
- Keep README.md up to date
- Document breaking changes

## Questions?

- Open an issue for bugs or feature requests
- Use discussions for general questions
- Check existing documentation first

## Recognition

Contributors will be acknowledged in:
- Release notes
- CONTRIBUTORS.md file
- Git commit history

Thank you for contributing to TradingBot! Your efforts help make algorithmic trading more accessible to everyone.
