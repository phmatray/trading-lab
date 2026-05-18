using System.Text.Json;
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Features.PriceFeed.Providers;
using Xunit;

namespace TradyStrat.Tests.Settings.Providers;

public class YahooMetadataParserTests
{
    private static JsonDocument Load(string fixture)
        => JsonDocument.Parse(File.ReadAllText(
            Path.Combine("Settings", "Providers", "Fixtures", fixture)));

    [Fact]
    public void Parses_eur_etp_metadata()
    {
        using var doc = Load("yahoo-quote-eur-etp.json");
        var meta = YahooParser.ParseMetadata("ETHE.PA", doc);

        meta.Ticker.ShouldBe("ETHE.PA");
        meta.Name.ShouldBe("WisdomTree Physical Ethereum");
        meta.Currency.ShouldBe("EUR");
        meta.Exchange.ShouldBe("Euronext Paris");
        meta.TimezoneId.ShouldBe("Europe/Paris");
    }

    [Fact]
    public void Throws_InstrumentNotFoundException_when_result_is_empty()
    {
        using var doc = Load("yahoo-quote-not-found.json");
        Should.Throw<InstrumentNotFoundException>(
            () => YahooParser.ParseMetadata("XYZ", doc));
    }

    [Fact]
    public void Throws_InstrumentMetadataIncompleteException_when_currency_missing()
    {
        using var doc = Load("yahoo-quote-incomplete.json");
        Should.Throw<InstrumentMetadataIncompleteException>(
            () => YahooParser.ParseMetadata("WEIRD", doc));
    }
}
