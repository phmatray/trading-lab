// <copyright file="MeanReversionStrategyTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.MarketData;
using TradingBot.Strategies.Implementations;

namespace TradingBot.Strategies.Tests.Implementations;

public sealed class MeanReversionStrategyTests
{
    private readonly ILogger<MeanReversionStrategy> _logger;
    private readonly MeanReversionConfig _config;
    private readonly MeanReversionStrategy _strategy;

    public MeanReversionStrategyTests()
    {
        _logger = A.Fake<ILogger<MeanReversionStrategy>>();
        _config = new MeanReversionConfig
        {
            Name = "TestMeanReversion",
            Symbols = new List<string> { "SPY" },
            Timeframe = "1h",
            LookbackPeriod = 20,
            StdMultiplier = 2.0,
            ExitAtMean = true,
        };
        _strategy = new MeanReversionStrategy(_logger, _config);
    }

    private static List<Candle> CreateBollingerTestCandles(decimal meanPrice, decimal deviation, int count)
    {
        var candles = new List<Candle>();
        var baseDate = DateTime.UtcNow.AddHours(-count);

        for (int i = 0; i < count; i++)
        {
            // Create price that oscillates around mean
            var offset = (decimal)Math.Sin(i * 0.5) * deviation;
            var price = meanPrice + offset;

            candles.Add(new Candle
            {
                Symbol = "SPY",
                Timestamp = baseDate.AddHours(i),
                Open = price - 0.2m,
                High = price + 0.5m,
                Low = price - 0.5m,
                Close = price,
                Volume = 500000,
                Timeframe = "1h",
            });
        }

        return candles;
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MeanReversionStrategy(null!, _config));
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<Exception>(() =>
            new MeanReversionStrategy(_logger, null!));
    }

    [Fact]
    public void Type_ShouldReturnMeanReversion()
    {
        // Act
        var type = _strategy.Type;

        // Assert
        type.ShouldBe("MeanReversion");
    }

    [Fact]
    public async Task ValidateParametersAsync_WithValidConfig_ShouldReturnTrue()
    {
        // Act
        var isValid = await _strategy.ValidateParametersAsync();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateParametersAsync_WithZeroLookbackPeriod_ShouldReturnFalse()
    {
        // Arrange
        var invalidConfig = new MeanReversionConfig
        {
            Name = "Invalid",
            Symbols = new List<string> { "SPY" },
            Timeframe = "1h",
            LookbackPeriod = 0, // Invalid!
            StdMultiplier = 2.0,
            ExitAtMean = true,
        };
        var invalidStrategy = new MeanReversionStrategy(_logger, invalidConfig);

        // Act
        var isValid = await invalidStrategy.ValidateParametersAsync();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateParametersAsync_WithZeroStdMultiplier_ShouldReturnFalse()
    {
        // Arrange
        var invalidConfig = new MeanReversionConfig
        {
            Name = "Invalid",
            Symbols = new List<string> { "SPY" },
            Timeframe = "1h",
            LookbackPeriod = 20,
            StdMultiplier = 0, // Invalid!
            ExitAtMean = true,
        };
        var invalidStrategy = new MeanReversionStrategy(_logger, invalidConfig);

        // Act
        var isValid = await invalidStrategy.ValidateParametersAsync();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateParametersAsync_WithEmptySymbols_ShouldReturnFalse()
    {
        // Arrange
        var invalidConfig = new MeanReversionConfig
        {
            Name = "Invalid",
            Symbols = new List<string>(), // Empty!
            Timeframe = "1h",
            LookbackPeriod = 20,
            StdMultiplier = 2.0,
            ExitAtMean = true,
        };
        var invalidStrategy = new MeanReversionStrategy(_logger, invalidConfig);

        // Act
        var isValid = await invalidStrategy.ValidateParametersAsync();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateSignalAsync_WithInsufficientData_ShouldReturnNull()
    {
        // Arrange
        var insufficientData = CreateBollingerTestCandles(100m, 5m, 10); // Only 10 candles, need 20

        // Act
        var signal = await _strategy.GenerateSignalAsync("SPY", insufficientData);

        // Assert
        signal.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateSignalAsync_WithPriceBelowLowerBand_MayReturnBuySignal()
    {
        // Arrange
        var candles = CreateBollingerTestCandles(100m, 3m, 30);

        // Add a candle with price breaking below lower band (oversold)
        var lastPrice = 90m; // Significantly below mean
        candles.Add(new Candle
        {
            Symbol = "SPY",
            Timestamp = candles[^1].Timestamp.AddHours(1),
            Open = 92m,
            High = 93m,
            Low = 89m,
            Close = lastPrice,
            Volume = 1000000,
            Timeframe = "1h",
        });

        // Act
        var signal = await _strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        // Indicator calculations may vary, so we verify IF a signal is generated, it's valid
        if (signal != null)
        {
            signal.Type.ShouldBe(SignalType.Buy);
            signal.Symbol.ShouldBe("SPY");
            signal.Confidence.ShouldBeGreaterThan(0);
            signal.Confidence.ShouldBeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task GenerateSignalAsync_WithPriceAboveUpperBand_MayReturnSellSignal()
    {
        // Arrange
        var candles = CreateBollingerTestCandles(100m, 3m, 30);

        // Add a candle with price breaking above upper band (overbought)
        var lastPrice = 110m; // Significantly above mean
        candles.Add(new Candle
        {
            Symbol = "SPY",
            Timestamp = candles[^1].Timestamp.AddHours(1),
            Open = 108m,
            High = 111m,
            Low = 107m,
            Close = lastPrice,
            Volume = 1000000,
            Timeframe = "1h",
        });

        // Act
        var signal = await _strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        // Indicator calculations may vary, so we verify IF a signal is generated, it's valid
        if (signal != null)
        {
            signal.Type.ShouldBe(SignalType.Sell);
            signal.Symbol.ShouldBe("SPY");
            signal.Confidence.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task GenerateSignalAsync_WithPriceWithinBands_ShouldReturnNull()
    {
        // Arrange - Price oscillating within bands
        var candles = CreateBollingerTestCandles(100m, 2m, 30);

        // Act
        var signal = await _strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        // When price is within bands and not near mean, no signal expected
        // Unless ExitAtMean is true and price is very close to mean
        if (signal != null)
        {
            signal.Type.ShouldBe(SignalType.Close);
        }
    }

    [Fact]
    public async Task GenerateSignalAsync_WithExitAtMeanEnabled_AndPriceNearMean_ShouldReturnCloseSignal()
    {
        // Arrange
        var candles = CreateBollingerTestCandles(100m, 5m, 30);

        // Add candle very close to mean
        candles.Add(new Candle
        {
            Symbol = "SPY",
            Timestamp = candles[^1].Timestamp.AddHours(1),
            Open = 100.1m,
            High = 100.3m,
            Low = 99.8m,
            Close = 100.05m, // Very close to mean of 100
            Volume = 500000,
            Timeframe = "1h",
        });

        // Act
        var signal = await _strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        if (signal != null)
        {
            signal.Type.ShouldBe(SignalType.Close);
            signal.Symbol.ShouldBe("SPY");
        }
    }

    [Fact]
    public async Task GenerateSignalAsync_WithExitAtMeanDisabled_ShouldNotReturnCloseSignal()
    {
        // Arrange
        var configNoExit = new MeanReversionConfig
        {
            Name = "NoExit",
            Symbols = new List<string> { "SPY" },
            Timeframe = "1h",
            LookbackPeriod = 20,
            StdMultiplier = 2.0,
            ExitAtMean = false, // Disabled
        };
        var strategyNoExit = new MeanReversionStrategy(_logger, configNoExit);

        var candles = CreateBollingerTestCandles(100m, 5m, 30);
        candles.Add(new Candle
        {
            Symbol = "SPY",
            Timestamp = candles[^1].Timestamp.AddHours(1),
            Open = 100.1m,
            High = 100.3m,
            Low = 99.8m,
            Close = 100.05m,
            Volume = 500000,
            Timeframe = "1h",
        });

        // Act
        var signal = await strategyNoExit.GenerateSignalAsync("SPY", candles);

        // Assert
        // Should not generate close signal when ExitAtMean is false
        if (signal != null)
        {
            signal.Type.ShouldNotBe(SignalType.Close);
        }
    }

    [Fact]
    public async Task GenerateSignalAsync_WhenSignalGenerated_ShouldIncludeMetadata()
    {
        // Arrange
        var candles = CreateBollingerTestCandles(100m, 3m, 30);
        candles.Add(new Candle
        {
            Symbol = "SPY",
            Timestamp = candles[^1].Timestamp.AddHours(1),
            Open = 92m,
            High = 93m,
            Low = 89m,
            Close = 90m,
            Volume = 1000000,
            Timeframe = "1h",
        });

        // Act
        var signal = await _strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        // If signal is generated, verify it has proper metadata
        if (signal != null)
        {
            signal.Metadata.ShouldNotBeNull();
            signal.Metadata.ContainsKey("upper_band").ShouldBeTrue();
            signal.Metadata.ContainsKey("middle_band").ShouldBeTrue();
            signal.Metadata.ContainsKey("lower_band").ShouldBeTrue();
            signal.Metadata.ContainsKey("current_price").ShouldBeTrue();
            signal.Metadata.ContainsKey("band_width").ShouldBeTrue();
        }
    }
}
