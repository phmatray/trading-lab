using Shouldly;
using TradyStrat.Domain.Suggestions;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class PromptFingerprintTests
{
    [Fact]
    public void Of_requires_non_empty_PromptHash()
    {
        Should.Throw<ArgumentException>(() =>
            PromptFingerprint.Of("", "env", "v1"));
        Should.Throw<ArgumentException>(() =>
            PromptFingerprint.Of(null!, "env", "v1"));
    }

    [Fact]
    public void Of_allows_empty_optional_components()
    {
        var fp = PromptFingerprint.Of("abc", "", "");
        fp.PromptHash.ShouldBe("abc");
        fp.EnvelopeHash.ShouldBe("");
        fp.PromptVersionHash.ShouldBe("");
    }

    [Fact]
    public void Of_normalizes_null_optional_components_to_empty_string()
    {
        var fp = PromptFingerprint.Of("abc", null!, null!);
        fp.EnvelopeHash.ShouldBe("");
        fp.PromptVersionHash.ShouldBe("");
    }
}
