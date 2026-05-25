using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    private static readonly JsonSerializerOptions SnapshotJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.ToTable("Suggestions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.InstrumentId);
        builder.Property(s => s.ForDate);
        builder.Property(s => s.Action);
        builder.Property(s => s.Rationale).HasMaxLength(4000).IsRequired();
        builder.Property(s => s.ThinkingText).HasMaxLength(20000);
        builder.Property(s => s.CreatedAt);

        builder.Property(s => s.Conviction)
               .HasConversion(c => c.Value, v => Conviction.Of(v));

        builder.OwnsOne(s => s.QuantityHint, q =>
        {
            q.Property(x => x.Value).HasColumnName("QuantityHint").HasColumnType("TEXT");
            q.Property(x => x.IsSpecified).HasColumnName("QuantityHintIsSpecified");
        });

        builder.OwnsOne(s => s.MaxPriceHint, p =>
        {
            p.OwnsOne(x => x.PerUnit, m =>
            {
                m.Property(x => x.Amount).HasColumnName("MaxPriceHint").HasColumnType("TEXT");
                m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
                 .HasColumnName("MaxPriceHintCurrency").HasMaxLength(3);
                m.Property(x => x.IsEmpty).HasColumnName("MaxPriceHintIsEmpty");
            });
        });

        builder.OwnsOne(s => s.Fingerprint, fp =>
        {
            fp.Property(x => x.PromptHash).HasColumnName("PromptHash").HasMaxLength(128).IsRequired();
            fp.Property(x => x.EnvelopeHash).HasColumnName("EnvelopeHash").HasMaxLength(128);
            fp.Property(x => x.PromptVersionHash).HasColumnName("PromptVersionHash").HasMaxLength(128);
        });

        builder.Property(s => s.Snapshot)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, SnapshotJson),
                   v => string.IsNullOrEmpty(v)
                            ? MarketSnapshot.Empty
                            : JsonSerializer.Deserialize<MarketSnapshot>(v, SnapshotJson) ?? MarketSnapshot.Empty)
               .HasColumnName("MarketSnapshotJson")
               .HasMaxLength(20000);

        builder.OwnsMany(s => s.Citations, c =>
        {
            c.ToTable("Citations");
            c.WithOwner().HasForeignKey("SuggestionId");
            c.Property<int>("Id").ValueGeneratedOnAdd();
            c.HasKey("Id");

            c.Property(x => x.Claim).HasMaxLength(2000);
            c.Property(x => x.Indicator).HasMaxLength(64);
            c.Property(x => x.Ticker).HasMaxLength(32);
            c.Property(x => x.Value).HasMaxLength(256);
        });
        builder.Navigation(s => s.Citations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<string>("CitationsJson").HasMaxLength(8000).HasDefaultValue("[]");

        builder.HasIndex(s => s.InstrumentId);
        builder.HasIndex(s => new { s.ForDate, s.InstrumentId }).IsUnique();
        builder.Ignore(s => s.OrderValue);
    }
}
