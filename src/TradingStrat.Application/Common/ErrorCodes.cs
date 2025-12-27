namespace TradingStrat.Application.Common;

/// <summary>
/// Centralized error codes for Application layer.
/// Organized by domain area for easy navigation and consistency.
/// Use these constants instead of magic strings for IntelliSense support and refactorability.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Portfolio management error codes.
    /// </summary>
    public static class Portfolio
    {
        public const string NotFound = "PORTFOLIO_NOT_FOUND";
        public const string CreationFailed = "PORTFOLIO_CREATION_FAILED";
        public const string NameConflict = "PORTFOLIO_NAME_CONFLICT";
        public const string UpdateFailed = "PORTFOLIO_UPDATE_FAILED";
        public const string DeleteFailed = "PORTFOLIO_DELETE_FAILED";
        public const string SnapshotFailed = "PORTFOLIO_SNAPSHOT_FAILED";
        public const string PerformanceFailed = "PORTFOLIO_PERFORMANCE_FAILED";
    }

    /// <summary>
    /// Position management error codes.
    /// </summary>
    public static class Position
    {
        public const string NotFound = "POSITION_NOT_FOUND";
        public const string AddFailed = "POSITION_ADD_FAILED";
        public const string UpdateFailed = "POSITION_UPDATE_FAILED";
        public const string DeleteFailed = "POSITION_DELETE_FAILED";
        public const string AlreadyExists = "POSITION_ALREADY_EXISTS";
    }

    /// <summary>
    /// Cash transaction error codes.
    /// </summary>
    public static class Cash
    {
        public const string TransactionFailed = "CASH_TRANSACTION_FAILED";
        public const string HistoryFailed = "CASH_HISTORY_FAILED";
        public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    }

    /// <summary>
    /// Historical data management error codes.
    /// </summary>
    public static class Data
    {
        public const string FetchFailed = "DATA_FETCH_FAILED";
        public const string NoHistoricalData = "NO_HISTORICAL_DATA";
        public const string TickerOrIsinRequired = "TICKER_OR_ISIN_REQUIRED";
        public const string IsinNotResolved = "ISIN_NOT_RESOLVED";
        public const string NoWorkingTicker = "NO_WORKING_TICKER";
        public const string StatusQueryFailed = "DATA_STATUS_QUERY_FAILED";
        public const string DeleteFailed = "DATA_DELETE_FAILED";
        public const string ExportFailed = "DATA_EXPORT_FAILED";
        public const string ImportFailed = "DATA_IMPORT_FAILED";
    }

    /// <summary>
    /// Backtesting error codes.
    /// </summary>
    public static class Backtest
    {
        public const string ExecutionFailed = "BACKTEST_EXECUTION_FAILED";
        public const string SaveFailed = "BACKTEST_SAVE_FAILED";
        public const string NotFound = "BACKTEST_NOT_FOUND";
        public const string DeleteFailed = "BACKTEST_DELETE_FAILED";
        public const string ArchiveQueryFailed = "BACKTEST_ARCHIVE_QUERY_FAILED";
    }

    /// <summary>
    /// Strategy management error codes.
    /// </summary>
    public static class Strategy
    {
        public const string NotFound = "STRATEGY_NOT_FOUND";
        public const string CreateFailed = "STRATEGY_CREATE_FAILED";
        public const string UpdateFailed = "STRATEGY_UPDATE_FAILED";
        public const string DeleteFailed = "STRATEGY_DELETE_FAILED";
        public const string CloneFailed = "STRATEGY_CLONE_FAILED";
        public const string RetrievalFailed = "STRATEGY_RETRIEVAL_FAILED";
        public const string InvalidDefinition = "INVALID_STRATEGY_DEFINITION";
        public const string ValidationFailed = "VALIDATION_FAILED";
    }

    /// <summary>
    /// Analysis and optimization error codes.
    /// </summary>
    public static class Analysis
    {
        public const string AnalysisFailed = "ANALYSIS_FAILED";
        public const string OptimizationFailed = "OPTIMIZATION_FAILED";
        public const string ComparisonFailed = "COMPARISON_FAILED";
    }

    /// <summary>
    /// Rebalancing error codes.
    /// </summary>
    public static class Rebalancing
    {
        public const string CalculationFailed = "REBALANCING_CALCULATION_FAILED";
        public const string InvalidAllocations = "INVALID_ALLOCATIONS";
    }

    /// <summary>
    /// Dashboard and statistics error codes.
    /// </summary>
    public static class Dashboard
    {
        public const string StatsFailed = "DASHBOARD_STATS_FAILED";
    }

    /// <summary>
    /// Common validation error codes.
    /// </summary>
    public static class Validation
    {
        public const string InvalidDateRange = "INVALID_DATE_RANGE";
        public const string FutureDateNotAllowed = "FUTURE_DATE_NOT_ALLOWED";
        public const string InvalidParameter = "INVALID_PARAMETER";
        public const string InvalidCommission = "INVALID_COMMISSION";
        public const string InvalidCapital = "INVALID_CAPITAL";
    }
}
