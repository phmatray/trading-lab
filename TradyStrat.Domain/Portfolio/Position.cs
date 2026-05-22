using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Position
{
    public PositionId   Id           { get; private set; }
    public InstrumentId InstrumentId { get; private set; }

    private readonly List<Lot>   _openLots = new();
    private readonly List<Trade> _trades   = new();

    private Money _realizedPnL = Money.Zero(Currency.Eur);

    public IReadOnlyList<Lot>   OpenLots   => _openLots;
    public IReadOnlyList<Trade> Trades     => _trades;
    public Money                RealizedPnL => _realizedPnL;

    public Quantity TotalQuantity
    {
        get
        {
            var sum = Quantity.Zero;
            foreach (var lot in _openLots)
                sum += lot.Quantity;
            return sum;
        }
    }

    public Money CostBasis
    {
        get
        {
            var sum = Money.Zero(Currency.Eur);
            foreach (var lot in _openLots)
                sum += lot.CostBasis;
            return sum;
        }
    }

    private Position() { }   // EF

    private Position(InstrumentId instrumentId)
    {
        Id           = PositionId.New();
        InstrumentId = instrumentId;
    }

    public static Position OpenFor(InstrumentId instrumentId) => new(instrumentId);
}
