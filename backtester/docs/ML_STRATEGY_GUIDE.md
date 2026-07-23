# Machine Learning Strategy Guide

## Understanding ML in Hexagonal Architecture

The hexagonal architecture handles machine learning predictions **differently** than traditional strategies due to the complexity of model training and walk-forward validation.

## Why ML Strategies Are Different

### Traditional Strategies (MA, RSI, MACD)
- Calculate indicators from historical data
- Generate signals based on mathematical rules
- **No state** - same inputs always produce same outputs
- **Fast** - can process thousands of bars per second

### Machine Learning Strategies
- Require **model training** on historical data
- Training can take seconds to minutes
- **Stateful** - model must be retrained periodically
- For proper backtesting, require **walk-forward validation**

## Walk-Forward Validation Problem

Proper ML backtesting requires retraining the model at each time step:

```
Time:     t1    t2    t3    t4    t5    t6
Data:     ●     ●     ●     ●     ●     ●
          |_____|
          Train    Predict at t2
                |_____|
                Train    Predict at t3
                     |_____|
                     Train    Predict at t4
```

**Problem**: Training 1000+ models for a backtest is computationally expensive (minutes to hours).

## How ML Predictions Work in Hexagonal Architecture

### For Live Analysis (✅ Implemented)

Use the **ILiveAnalysisUseCase** for current position analysis:

```csharp
// Application Layer - AnalyzeCurrentPositionUseCase.cs
public async Task<LiveAnalysisResult> ExecuteAsync(AnalysisCommand command)
{
    // 1. Fetch latest data
    var latestPrice = await _marketDataPort.FetchLatestPriceAsync(ticker);

    // 2. Calculate technical indicators
    var featureEngine = new FeatureEngineering(
        completeData.AsReadOnly(),
        _indicatorCalculator);  // ← No reflection hack!

    // 3. Build feature matrix (26 technical indicators)
    var features = featureEngine.BuildFeatureMatrix();

    // 4. Train ML model via IMLModelPort
    var model = _mlModelPort.TrainModel(trainingData, mlConfig);

    // 5. Predict next-day return
    var predictedReturn = _mlModelPort.Predict(model, currentFeatures);

    // 6. Convert prediction to Buy/Sell/Hold signal
    var signal = _thresholds.ConvertPredictionToSignal(predictedReturn);

    return new LiveAnalysisResult(signal, predictedReturn, currentFeatures);
}
```

**Usage from CLI:**
```
Choose option: 3 (Analyze Current Position)
```

### For Backtesting (⚠️ Not Implemented)

The `MachineLearningStrategy` in Domain is a **placeholder** that:
- Always returns `Hold` signals
- Displays a warning during initialization
- Prevents errors when "ml" is selected

```csharp
// Domain Layer - MachineLearningStrategy.cs (Placeholder)
public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
{
    // Always return Hold - ML backtesting not implemented
    return new TradeSignal(
        SignalType.Hold,
        ClosePrices[currentIndex],
        0,
        "ML backtesting not available - use live analysis instead");
}
```

When you try to backtest with ML, you'll see:

```
╭─Warning──────────────────────────────────────────────────────────────────╮
│ Machine Learning Strategy is not available for backtesting.              │
│                                                                          │
│ ML strategies require walk-forward model training at each time step,    │
│ which is computationally expensive and not yet implemented.              │
│                                                                          │
│ For ML predictions, use the 'Analyze Current Position' feature instead. │
│                                                                          │
│ This backtest will run with HOLD signals only.                          │
╰──────────────────────────────────────────────────────────────────────────╯
```

## Architecture Benefits

### ✅ Eliminated Reflection Hack

**Before** (Monolith):
```csharp
// LiveAnalysisEngine had to use reflection to access private field
var field = typeof(MachineLearningStrategy)
    .GetField("_featureEngine", BindingFlags.NonPublic | BindingFlags.Instance);
var featureEngine = (FeatureEngineering)field!.GetValue(strategy)!;
```

**After** (Hexagonal):
```csharp
// Clean dependency injection
var featureEngine = new FeatureEngineering(
    completeData.AsReadOnly(),
    _indicatorCalculator);  // ← Injected via DI
```

### ✅ Separation of Concerns

- **Domain Layer**: Contains strategies with pure business logic
- **Application Layer**: Orchestrates ML training via `IMLModelPort`
- **Infrastructure Layer**: Implements ML.NET training (`MlNetModelAdapter`)

### ✅ Testability

```csharp
// Test live analysis with mock ML model port
var mockMLModelPort = new Mock<IMLModelPort>();
mockMLModelPort
    .Setup(x => x.Predict(It.IsAny<ITransformer>(), It.IsAny<MarketFeatures>()))
    .Returns(0.025f);  // Predict +2.5% return

var useCase = new AnalyzeCurrentPositionUseCase(
    _historicalDataPort,
    _marketDataPort,
    mockMLModelPort.Object,  // ← Mocked ML
    _indicatorCalculator);
```

## Future: Implementing ML Backtesting

To implement proper ML backtesting, you would need:

### Option 1: Walk-Forward Validation Use Case

Create a dedicated `IMLBacktestUseCase` that:
1. Splits data into chunks (e.g., 250-day windows)
2. Trains model on each chunk
3. Predicts next period
4. Repeats for entire dataset

**Pros**: Accurate representation of real-world performance
**Cons**: Very slow (minutes to hours for long backtests)

### Option 2: Pre-Trained Model Strategy

Create a strategy that uses a pre-trained model:
```csharp
public class PreTrainedMLStrategy : BaseStrategy
{
    private readonly ITransformer _preTrainedModel;

    public PreTrainedMLStrategy(
        IIndicatorCalculator indicatorCalculator,
        ITransformer preTrainedModel)
        : base(indicatorCalculator)
    {
        _preTrainedModel = preTrainedModel;
    }

    public override TradeSignal GenerateSignal(...)
    {
        var features = CalculateFeatures(currentIndex);
        var prediction = _mlModelPort.Predict(_preTrainedModel, features);
        return ConvertToSignal(prediction);
    }
}
```

**Pros**: Fast backtesting
**Cons**: Look-ahead bias (model trained on all data, not walk-forward)

### Option 3: Cached Predictions

Pre-compute all predictions and store them:
```csharp
// Pre-compute predictions once
var allPredictions = await _mlBacktestService.ComputeWalkForwardPredictionsAsync(ticker);

// Use cached predictions during backtest
public override TradeSignal GenerateSignal(int currentIndex, ...)
{
    var prediction = _cachedPredictions[currentIndex];
    return ConvertToSignal(prediction);
}
```

**Pros**: Accurate + Fast after initial computation
**Cons**: Requires pre-computation and storage

## Recommended Workflow

For now, use this workflow:

### 1. Backtest Traditional Strategies
```
dotnet run
> 2. Run Backtest
> Strategy: ma (or rsi, macd)
```

Use MA/RSI/MACD to find promising ticker symbols and time periods.

### 2. Analyze with ML
```
dotnet run
> 3. Analyze Current Position
> Ticker: CON3.L
```

Use ML for live analysis on tickers that showed promise in traditional backtests.

### 3. Combine Insights

- Traditional backtests show historical strategy performance
- ML analysis provides current buy/sell recommendations
- Together they give a complete picture

## Technical Details

### 26 Technical Indicators (Features)

**Price-based (5)**:
- Daily Return, Log Return, High-Low Range, Open-Close Range, Price Position

**Moving Averages (6)**:
- SMA 5/10/20, EMA 12/26, Price to SMA20

**Momentum (4)**:
- RSI 14, Momentum 5, ROC 10, Stochastic RSI

**MACD (3)**:
- MACD line, Signal line, Histogram

**Volatility (4)**:
- StdDev 10/20, ATR 14, Bollinger Position

**Volume (4)**:
- Volume Change, Volume MA 10, Volume Ratio, Price-Volume Correlation

### ML Algorithm

- **Model**: FastTree Gradient Boosting (ML.NET)
- **Task**: Regression (predict next-day return)
- **Hyperparameters**:
  - Number of Trees: 100
  - Number of Leaves: 31
  - Learning Rate: 0.1
  - Min Examples Per Leaf: 20

### Configuration

All ML settings in `appsettings.json`:

```json
{
  "Trading": {
    "MachineLearning": {
      "MinTrainingBars": 100,
      "DefaultThresholds": {
        "BuyThreshold": 0.01,
        "SellThreshold": -0.01
      },
      "ModelParameters": {
        "NumberOfLeaves": 31,
        "MinimumExampleCountPerLeaf": 20,
        "LearningRate": 0.1,
        "NumberOfTrees": 100
      }
    }
  }
}
```

## Summary

| Use Case | Implementation | Status |
|----------|---------------|--------|
| **Live Analysis** | ILiveAnalysisUseCase | ✅ Fully Implemented |
| **Backtesting** | MachineLearningStrategy (placeholder) | ⚠️ Returns Hold signals only |
| **Walk-Forward Backtest** | Not implemented | ❌ Future enhancement |

**Key Takeaway**: Use the **"Analyze Current Position"** menu option for ML predictions. The hexagonal architecture makes this clean, testable, and free of reflection hacks!
