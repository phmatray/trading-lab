using Shouldly;
using TradyStrat.Domain.Settings;
using TradyStrat.Domain.Settings.Anthropic;
using Xunit;

namespace TradyStrat.Domain.Tests.Settings.Anthropic;

public class AnthropicVoTests
{
    [Fact] public void AnthropicModel_trims_input()
        => AnthropicModel.Of("  claude-opus-4-7  ").Value.ShouldBe("claude-opus-4-7");

    [Fact] public void AnthropicModel_rejects_blank()
        => Should.Throw<SettingValidationException>(() => AnthropicModel.Of(" "));

    [Theory]
    [InlineData(0)]
    [InlineData(100_001)]
    public void MaxTokens_rejects_out_of_range(int n)
        => Should.Throw<SettingValidationException>(() => MaxTokens.Of(n));

    [Fact] public void MaxTokens_accepts_in_range()
        => MaxTokens.Of(1500).Value.ShouldBe(1500);

    [Theory]
    [InlineData(1023)]
    [InlineData(16_001)]
    public void ThinkingBudget_rejects_out_of_range(int n)
        => Should.Throw<SettingValidationException>(() => ThinkingBudget.Of(n));

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void MaxParallel_rejects_out_of_range(int n)
        => Should.Throw<SettingValidationException>(() => MaxParallelSuggestions.Of(n));
}
