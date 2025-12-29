using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the StrategyForm component - dynamic form for strategy configuration.
/// </summary>
public class StrategyFormTests : BunitTestContext
{
    [Fact]
    public void StrategyForm_InitialRender_DisplaysMovingAverageByDefault()
    {
        // Arrange & Act
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();

        // Assert
        cut.Markup.ShouldContain("Strategy Configuration");
        cut.Markup.ShouldContain("Strategy Type");

        // Should display MA parameter inputs
        IElement fastPeriodInput = cut.Find("#fast-period");
        fastPeriodInput.ShouldNotBeNull();

        IElement slowPeriodInput = cut.Find("#slow-period");
        slowPeriodInput.ShouldNotBeNull();
    }

    [Fact]
    public void StrategyForm_StrategySelector_HasAllStrategyOptions()
    {
        // Arrange & Act
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();

        // Assert
        IElement select = cut.Find("#strategy-select");
        cut.Markup.ShouldContain("Moving Average Crossover");
        cut.Markup.ShouldContain("RSI Strategy");
        cut.Markup.ShouldContain("MACD Strategy");
        cut.Markup.ShouldContain("ML FastTree");
        cut.Markup.ShouldContain("Ichimoku Cloud");
    }

    [Fact]
    public void StrategyForm_SelectRSI_DisplaysRSIParameters()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();
        IElement select = cut.Find("#strategy-select");

        // Act
        select.Change("rsi");

        // Assert
        IElement periodInput = cut.Find("#rsi-period");
        periodInput.ShouldNotBeNull();

        IElement oversoldInput = cut.Find("#oversold");
        oversoldInput.ShouldNotBeNull();

        IElement overboughtInput = cut.Find("#overbought");
        overboughtInput.ShouldNotBeNull();

        // Should NOT display MA inputs
        cut.FindAll("#fast-period").ShouldBeEmpty();
    }

    [Fact]
    public void StrategyForm_SelectMACD_DisplaysMACDParameters()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();
        IElement select = cut.Find("#strategy-select");

        // Act
        select.Change("macd");

        // Assert
        IElement fastInput = cut.Find("#macd-fast");
        fastInput.ShouldNotBeNull();

        IElement slowInput = cut.Find("#macd-slow");
        slowInput.ShouldNotBeNull();

        IElement signalInput = cut.Find("#macd-signal");
        signalInput.ShouldNotBeNull();
    }

    [Fact]
    public void StrategyForm_SelectML_DisplaysMLParameters()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();
        IElement select = cut.Find("#strategy-select");

        // Act
        select.Change("ml");

        // Assert
        IElement buyThresholdInput = cut.Find("#buy-threshold");
        buyThresholdInput.ShouldNotBeNull();

        IElement sellThresholdInput = cut.Find("#sell-threshold");
        sellThresholdInput.ShouldNotBeNull();
    }

    [Fact]
    public void StrategyForm_SelectIchimoku_DisplaysIchimokuParameters()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();
        IElement select = cut.Find("#strategy-select");

        // Act
        select.Change("ichimoku");

        // Assert
        cut.Find("#tenkan-period").ShouldNotBeNull();
        cut.Find("#kijun-period").ShouldNotBeNull();
        cut.Find("#senkou-b-period").ShouldNotBeNull();
        cut.Find("#displacement").ShouldNotBeNull();
        cut.Find("#cross-lookback").ShouldNotBeNull();
        cut.Find("#exit-mode").ShouldNotBeNull();
        cut.Find("#entry-mode").ShouldNotBeNull();
        cut.Find("#risk-percentage").ShouldNotBeNull();
    }

    [Fact]
    public void StrategyForm_ChangeStrategy_InvokesCallback()
    {
        // Arrange
        string? selectedStrategy = null;
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategyChanged, strategy => selectedStrategy = strategy));

        IElement select = cut.Find("#strategy-select");

        // Act
        select.Change("rsi");

        // Assert
        selectedStrategy.ShouldBe("rsi");
    }

    [Fact]
    public void StrategyForm_ChangeStrategy_InvokesParametersChangedCallback()
    {
        // Arrange
        Dictionary<string, object>? parameters = null;
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(builder => builder
            .Add(p => p.ParametersChanged, p => parameters = p));

        IElement select = cut.Find("#strategy-select");

        // Act
        select.Change("rsi");

        // Assert
        parameters.ShouldNotBeNull();
        parameters.ContainsKey("Period").ShouldBeTrue();
        parameters.ContainsKey("OversoldThreshold").ShouldBeTrue();
        parameters.ContainsKey("OverboughtThreshold").ShouldBeTrue();
    }

    [Fact]
    public void StrategyForm_MAParameters_HaveCorrectDefaultValues()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ma"));

        // Act
        StrategyForm instance = cut.Instance;
        Dictionary<string, object> currentParams = instance.GetCurrentParameters();

        // Assert
        currentParams["FastPeriod"].ShouldBe(20);
        currentParams["SlowPeriod"].ShouldBe(50);
    }

    [Fact]
    public void StrategyForm_RSIParameters_HaveCorrectDefaultValues()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "rsi"));

        // Act
        StrategyForm instance = cut.Instance;
        Dictionary<string, object> currentParams = instance.GetCurrentParameters();

        // Assert
        currentParams["Period"].ShouldBe(14);
        currentParams["OversoldThreshold"].ShouldBe(30m);
        currentParams["OverboughtThreshold"].ShouldBe(70m);
    }

    [Fact]
    public void StrategyForm_MACDParameters_HaveCorrectDefaultValues()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "macd"));

        // Act
        StrategyForm instance = cut.Instance;
        Dictionary<string, object> currentParams = instance.GetCurrentParameters();

        // Assert
        currentParams["FastPeriod"].ShouldBe(12);
        currentParams["SlowPeriod"].ShouldBe(26);
        currentParams["SignalPeriod"].ShouldBe(9);
    }

    [Fact]
    public void StrategyForm_MLParameters_HaveCorrectDefaultValues()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ml"));

        // Act
        StrategyForm instance = cut.Instance;
        Dictionary<string, object> currentParams = instance.GetCurrentParameters();

        // Assert
        // ML thresholds are divided by 100 in GetCurrentParameters
        currentParams["BuyThreshold"].ShouldBe(0.01m);
        currentParams["SellThreshold"].ShouldBe(-0.01m);
    }

    [Fact]
    public void StrategyForm_IchimokuParameters_HaveCorrectDefaultValues()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ichimoku"));

        // Act
        StrategyForm instance = cut.Instance;
        Dictionary<string, object> currentParams = instance.GetCurrentParameters();

        // Assert
        currentParams["TenkanPeriod"].ShouldBe(9);
        currentParams["KijunPeriod"].ShouldBe(26);
        currentParams["SenkouBPeriod"].ShouldBe(52);
        currentParams["Displacement"].ShouldBe(26);
        currentParams["ExitMode"].ShouldBe("CloseBelowKijun");
        currentParams["EntryMode"].ShouldBe("AllConditionsOnly");
        currentParams["CrossLookbackDays"].ShouldBe(5);
        currentParams["RiskPercentage"].ShouldBe(0.02m); // 2.0 / 100
    }

    [Fact]
    public void StrategyForm_MAInputs_HaveCorrectValidationAttributes()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ma"));

        // Act
        IElement fastPeriod = cut.Find("#fast-period");
        IElement slowPeriod = cut.Find("#slow-period");

        // Assert
        fastPeriod.GetAttribute("type").ShouldBe("number");
        fastPeriod.GetAttribute("min").ShouldBe("1");
        fastPeriod.GetAttribute("max").ShouldBe("200");

        slowPeriod.GetAttribute("type").ShouldBe("number");
        slowPeriod.GetAttribute("min").ShouldBe("1");
        slowPeriod.GetAttribute("max").ShouldBe("200");
    }

    [Fact]
    public void StrategyForm_RSIInputs_HaveCorrectValidationAttributes()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "rsi"));

        // Act
        IElement periodInput = cut.Find("#rsi-period");
        IElement oversoldInput = cut.Find("#oversold");
        IElement overboughtInput = cut.Find("#overbought");

        // Assert
        periodInput.GetAttribute("min").ShouldBe("1");
        periodInput.GetAttribute("max").ShouldBe("100");

        oversoldInput.GetAttribute("min").ShouldBe("0");
        oversoldInput.GetAttribute("max").ShouldBe("100");

        overboughtInput.GetAttribute("min").ShouldBe("0");
        overboughtInput.GetAttribute("max").ShouldBe("100");
    }

    [Fact]
    public void StrategyForm_MLInputs_HaveStepAttribute()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ml"));

        // Act
        IElement buyThreshold = cut.Find("#buy-threshold");
        IElement sellThreshold = cut.Find("#sell-threshold");

        // Assert
        buyThreshold.GetAttribute("step").ShouldBe("0.1");
        buyThreshold.GetAttribute("min").ShouldBe("0");
        buyThreshold.GetAttribute("max").ShouldBe("10");

        sellThreshold.GetAttribute("step").ShouldBe("0.1");
        sellThreshold.GetAttribute("min").ShouldBe("-10");
        sellThreshold.GetAttribute("max").ShouldBe("0");
    }

    [Fact]
    public void StrategyForm_IchimokuExitMode_HasCorrectOptions()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ichimoku"));

        // Act & Assert
        cut.Markup.ShouldContain("Close Below Kijun");
        cut.Markup.ShouldContain("Price Into Kumo");
        cut.Markup.ShouldContain("Bearish Cross");
    }

    [Fact]
    public void StrategyForm_IchimokuEntryMode_HasCorrectOptions()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "ichimoku"));

        // Act & Assert
        cut.Markup.ShouldContain("All Conditions");
        cut.Markup.ShouldContain("Recent Cross Required");
    }

    [Fact]
    public void StrategyForm_SwitchBetweenStrategies_UpdatesParametersCorrectly()
    {
        // Arrange
        Dictionary<string, object>? parameters = null;
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(builder => builder
            .Add(p => p.ParametersChanged, p => parameters = p));

        IElement select = cut.Find("#strategy-select");

        // Act - Switch from MA to RSI
        select.Change("rsi");

        // Assert
        parameters.ShouldNotBeNull();
        parameters.ContainsKey("Period").ShouldBeTrue();
        parameters.ContainsKey("FastPeriod").ShouldBeFalse(); // MA parameter should not exist

        // Act - Switch from RSI to MACD
        select.Change("macd");

        // Assert
        parameters.ContainsKey("FastPeriod").ShouldBeTrue(); // MACD has FastPeriod
        parameters.ContainsKey("SlowPeriod").ShouldBeTrue();
        parameters.ContainsKey("SignalPeriod").ShouldBeTrue();
        parameters.ContainsKey("Period").ShouldBeFalse(); // RSI parameter should not exist
    }

    [Fact]
    public void StrategyForm_GetCurrentParameters_ReturnsEmptyForUnknownStrategy()
    {
        // Arrange
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>(parameters => parameters
            .Add(p => p.SelectedStrategy, "unknown"));

        // Act
        StrategyForm instance = cut.Instance;
        Dictionary<string, object> currentParams = instance.GetCurrentParameters();

        // Assert
        currentParams.ShouldBeEmpty();
    }

    [Fact]
    public void StrategyForm_HasCardStyling()
    {
        // Arrange & Act
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();

        // Assert
        IElement card = cut.Find(".card");
        card.ShouldNotBeNull();
    }

    [Fact]
    public void StrategyForm_StrategySelector_HasCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<StrategyForm> cut = Render<StrategyForm>();

        // Assert
        IElement select = cut.Find("#strategy-select");
        select.ClassList.ShouldContain("w-full");
        select.ClassList.ShouldContain("rounded-lg");
    }
}
