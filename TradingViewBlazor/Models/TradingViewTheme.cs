using System.Text.Json.Serialization;

namespace TradingViewBlazor.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TradingViewTheme>))]
public enum TradingViewTheme
{
    Light,
    Dark
}