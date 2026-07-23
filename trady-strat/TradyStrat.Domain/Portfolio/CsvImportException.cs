using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Portfolio;

public sealed class CsvImportException(string message, int? lineNumber = null)
    : TradyStratException(lineNumber.HasValue ? $"line {lineNumber}: {message}" : message)
{
    public int? LineNumber { get; } = lineNumber;
}
