using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Strategies;
using TradingStrat.Web.Components.Base;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyComparison : BaseComponent
{
    [Inject] private IMultiStrategyComparisonUseCase ComparisonUseCase { get; set; } = null!;
    [Inject] private IStrategyRegistry StrategyRegistry { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;
    [Inject] private StrategyParameterDefaults ParameterDefaults { get; set; } = null!;

    private MultiStrategyComparisonResult? _comparisonResult;
    private bool _isLoading;
    private string? _errorMessage;

    private readonly List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Compare Strategies", Href = "/strategies/compare" }
    };

    // Form inputs
    private string _ticker = "";
    private DateTime _startDate = DateTime.Today.AddYears(-2);
    private DateTime _endDate = DateTime.Today;
    private decimal _initialCapital = 10000m;
    private decimal _commissionPercentage = 0.1m;
    private decimal _minimumCommission = 1.0m;

    // Selected strategies (max 5)
    private readonly List<SelectedStrategy> _selectedStrategies = new();
    private const int MaxStrategies = 5;

    protected override void OnInitialized()
    {
        // Initialize with defaults from configuration
        _ticker = Configuration.Value.DefaultTicker;
        _initialCapital = Configuration.Value.Backtest.InitialCapital;
        _commissionPercentage = Configuration.Value.Backtest.CommissionPercentage;
        _minimumCommission = Configuration.Value.Backtest.MinimumCommission;

        // Add one default strategy to start
        AddStrategy();
    }

    private void AddStrategy()
    {
        if (_selectedStrategies.Count >= MaxStrategies)
        {
            return;
        }

        _selectedStrategies.Add(new SelectedStrategy
        {
            Id = Guid.NewGuid(),
            StrategyType = StrategyType.RSI,
            Parameters = new Dictionary<string, object>
            {
                ["Period"] = 14,
                ["Oversold"] = 30,
                ["Overbought"] = 70
            }
        });
    }

    private void RemoveStrategy(Guid id)
    {
        _selectedStrategies.RemoveAll(s => s.Id == id);
    }

    private void OnStrategyTypeChanged(Guid id, string value)
    {
        SelectedStrategy? strategy = _selectedStrategies.FirstOrDefault(s => s.Id == id);
        if (strategy == null)
        {
            return;
        }

        if (StrategyRegistry.TryParseStrategyType(value, out StrategyType strategyType))
        {
            strategy.StrategyType = strategyType;

            // Reset parameters to defaults for the new strategy type
            strategy.Parameters = GetDefaultParameters(strategyType);
        }
    }

    private Dictionary<string, object> GetDefaultParameters(StrategyType strategyType)
    {
        // Use centralized defaults service - single source of truth
        return ParameterDefaults.GetAllDefaults(strategyType);
    }

    private async Task RunComparisonAsync()
    {
        if (_selectedStrategies.Count < 2)
        {
            _errorMessage = "Please select at least 2 strategies to compare.";
            return;
        }

        try
        {
            _isLoading = true;
            _errorMessage = null;
            _comparisonResult = null;
            StateHasChanged();

            // Convert selected strategies to command format
            List<StrategyConfiguration> strategyConfigs = _selectedStrategies
                .Select(s => new StrategyConfiguration(
                    StrategyType: s.StrategyType.ToString().ToLowerInvariant(),
                    Parameters: s.Parameters,
                    CustomStrategyId: null
                ))
                .ToList();

            MultiStrategyComparisonCommand command = new(
                Ticker: _ticker,
                Strategies: strategyConfigs,
                StartDate: _startDate,
                EndDate: _endDate,
                InitialCapital: _initialCapital,
                CommissionPercentage: _commissionPercentage,
                MinimumCommission: _minimumCommission
            );

            // Create progress reporter
            IProgress<string> progress = new Progress<string>(message =>
            {
                InvokeAsync(StateHasChanged);
            });

            _comparisonResult = await ComparisonUseCase.ExecuteAsync(command, progress);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to run strategy comparison");
            _errorMessage = "Failed to run comparison. Please check your inputs and try again.";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private string GetStrategyDisplayName(SelectedStrategy strategy)
    {
        return strategy.StrategyType.ToString();
    }

    private class SelectedStrategy
    {
        public Guid Id { get; set; }
        public StrategyType StrategyType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
