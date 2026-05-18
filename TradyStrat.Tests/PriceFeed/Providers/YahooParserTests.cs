using System.Text.Json;
using TradyStrat.Features.PriceFeed.Providers;
using Shouldly;
using TradyStrat.Domain.Exceptions;
using Xunit;

namespace TradyStrat.Tests.PriceFeed.Providers;

public class YahooParserTests
{
    private static JsonDocument Load(string fixture)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine("PriceFeed", "Fixtures", fixture)));

    [Fact]
    public void Parses_three_daily_bars_with_correct_decimals()
    {
        using var doc = Load("yahoo-con3-mini.json");
        var bars = YahooParser.ParseDaily("CON3.DE", doc);

        bars.Count.ShouldBe(3);
        bars[0].Ticker.ShouldBe("CON3.DE");
        bars[0].Close.ShouldBe(4.18m);
        bars[1].Volume.ShouldBe(95000L);
        bars[2].High.ShouldBe(4.30m);
    }

    [Fact]
    public void Returns_empty_list_when_payload_has_no_timestamps()
    {
        using var doc = Load("yahoo-empty.json");
        YahooParser.ParseDaily("CON3.DE", doc).ShouldBeEmpty();
    }

    [Fact]
    public void Throws_PriceFeedUnavailableException_for_malformed_payload()
    {
        using var doc = Load("yahoo-malformed.json");
        Should.Throw<PriceFeedUnavailableException>(() => YahooParser.ParseDaily("CON3.DE", doc));
    }

    [Fact]
    public void Skips_bars_with_null_close_values()
    {
        using var doc = JsonDocument.Parse("""
        {"chart":{"result":[{"timestamp":[1,2,3],"indicators":{"quote":[
          {"open":[1,2,3],"high":[1,2,3],"low":[1,2,3],"close":[1,null,3],"volume":[10,20,30]}
        ]}}],"error":null}}
        """);

        var bars = YahooParser.ParseDaily("X", doc);
        bars.Count.ShouldBe(2);
    }
}
