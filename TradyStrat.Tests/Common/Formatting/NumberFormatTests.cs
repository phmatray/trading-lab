using Shouldly;
using TradyStrat.Common.Formatting;
using Xunit;

namespace TradyStrat.Tests.Common.Formatting;

public class NumberFormatTests
{
    [Fact] public void Eur_Large_NoDecimals()        => NumberFormat.Eur(31911.42m).ShouldBe("€31 911");
    [Fact] public void Eur_WholeUnderThousand()      => NumberFormat.Eur(42m).ShouldBe("€42");
    [Fact] public void Eur_Zero()                    => NumberFormat.Eur(0m).ShouldBe("€0");
    [Fact] public void Eur_FractionUnderThousand()   => NumberFormat.Eur(0.59m).ShouldBe("€0,59");
    [Fact] public void Eur_FractionJustUnder()       => NumberFormat.Eur(999.5m).ShouldBe("€999,50");
    [Fact] public void Eur_WholeThousand()           => NumberFormat.Eur(1000m).ShouldBe("€1 000");

    [Fact] public void EurBody_StripsSymbol()        => NumberFormat.EurBody(31911m).ShouldBe("31 911");

    [Fact] public void SignedEur_Negative_RealMinus()=> NumberFormat.SignedEur(-46048m).ShouldBe("−€46 048");
    [Fact] public void SignedEur_Positive()          => NumberFormat.SignedEur(19685m).ShouldBe("+€19 685");
    [Fact] public void SignedEur_Zero()              => NumberFormat.SignedEur(0m).ShouldBe("+€0");

    [Fact] public void Qty_Whole()                   => NumberFormat.Qty(63650m).ShouldBe("63 650");
    [Fact] public void Qty_Fractional_TrimsZeros()   => NumberFormat.Qty(63650.5m).ShouldBe("63 650,5");

    [Fact] public void Pct_OneDecimal_NarrowSpace()  => NumberFormat.Pct(16.04m).ShouldBe("16,0 %");

    [Fact] public void Price_TwoDecimals_Prefix()    => NumberFormat.Price(203.04m, "$").ShouldBe("$203,04");
    [Fact] public void Price_GroupedAbove1000()      => NumberFormat.Price(1203.04m, "€").ShouldBe("€1 203,04");
}
