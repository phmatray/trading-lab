using Shouldly;
using TradyStrat.Domain.Settings;
using TradyStrat.Domain.Settings.Polymarket;
using Xunit;

namespace TradyStrat.Domain.Tests.Settings.Polymarket;

public class PolymarketVoTests
{
    [Fact] public void SearchQueries_trims_and_lowercases()
        => SearchQueries.Of(["  BITCOIN ", "Eth"]).Values.ShouldBe(["bitcoin", "eth"]);

    [Fact] public void SearchQueries_rejects_empty_list()
        => Should.Throw<SettingValidationException>(() => SearchQueries.Of([]));

    [Fact] public void SearchQueries_rejects_blank_entries()
        => Should.Throw<SettingValidationException>(() => SearchQueries.Of(["btc", "  "]));

    [Fact] public void MaxMarkets_rejects_zero()
        => Should.Throw<SettingValidationException>(() => MaxMarkets.Of(0));

    [Fact] public void MinVolumeUsd_rejects_negative()
        => Should.Throw<SettingValidationException>(() => MinVolumeUsd.Of(-1m));

    [Fact] public void MaxHorizonDays_rejects_zero()
        => Should.Throw<SettingValidationException>(() => MaxHorizonDays.Of(0));
}
