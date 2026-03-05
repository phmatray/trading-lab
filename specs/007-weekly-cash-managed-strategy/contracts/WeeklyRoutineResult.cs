// <copyright file="WeeklyRoutineResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Strategies;

/// <summary>
/// Result object returned by weekly routine execution.
/// Contains execution details, orders placed, and state changes.
/// </summary>
public sealed class WeeklyRoutineResult
{
    /// <summary>
    /// Gets or sets the strategy identifier that was executed.
    /// </summary>
    public required Guid StrategyId { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutionTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the routine executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether buy conditions were met.
    /// </summary>
    public bool BuyConditionsMet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sell conditions were met.
    /// </summary>
    public bool SellConditionsMet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a buy order was placed.
    /// </summary>
    public bool BuyOrderPlaced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a sell order was placed.
    /// </summary>
    public bool SellOrderPlaced { get; set; }

    /// <summary>
    /// Gets or sets the buy order ID if a buy was executed.
    /// </summary>
    public Guid? BuyOrderId { get; set; }

    /// <summary>
    /// Gets or sets the sell order ID if a sell was executed.
    /// </summary>
    public Guid? SellOrderId { get; set; }

    /// <summary>
    /// Gets or sets the buy amount in dollars (if buy order placed).
    /// </summary>
    public decimal BuyAmount { get; set; }

    /// <summary>
    /// Gets or sets the sell quantity in shares (if sell order placed).
    /// </summary>
    public decimal SellQuantity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cash buffer adjustment was needed.
    /// </summary>
    public bool CashBufferAdjustmentNeeded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cash buffer adjustment was executed.
    /// </summary>
    public bool CashBufferAdjustmentExecuted { get; set; }

    /// <summary>
    /// Gets or sets the cash buffer adjustment order ID if adjustment was made.
    /// </summary>
    public Guid? AdjustmentOrderId { get; set; }

    /// <summary>
    /// Gets or sets the cash ratio before execution.
    /// </summary>
    public decimal CashRatioBefore { get; set; }

    /// <summary>
    /// Gets or sets the cash ratio after execution.
    /// </summary>
    public decimal CashRatioAfter { get; set; }

    /// <summary>
    /// Gets or sets the total equity before execution.
    /// </summary>
    public decimal TotalEquityBefore { get; set; }

    /// <summary>
    /// Gets or sets the total equity after execution (estimated if orders pending).
    /// </summary>
    public decimal TotalEquityAfter { get; set; }

    /// <summary>
    /// Gets or sets the current MA20 value used in decision making.
    /// </summary>
    public decimal? MA20Value { get; set; }

    /// <summary>
    /// Gets or sets the current underlying asset price.
    /// </summary>
    public decimal CurrentUnderlyingPrice { get; set; }

    /// <summary>
    /// Gets or sets the current ETP price.
    /// </summary>
    public decimal CurrentEtpPrice { get; set; }

    /// <summary>
    /// Gets or sets the days below MA20 counter value.
    /// </summary>
    public int DaysBelowMA20 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether breakout conditions were met (if breakout rule enabled).
    /// </summary>
    public bool BreakoutConditionsMet { get; set; }

    /// <summary>
    /// Gets or sets the breakout multiplier applied (1.0 if no breakout, 2.0 if breakout rule triggered).
    /// </summary>
    public decimal BreakoutMultiplierApplied { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets additional execution notes or warnings.
    /// </summary>
    public List<string> ExecutionNotes { get; set; } = new();

    /// <summary>
    /// Adds an execution note to the result.
    /// </summary>
    /// <param name="note">The note to add.</param>
    public void AddNote(string note)
    {
        ExecutionNotes.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {note}");
    }

    /// <summary>
    /// Marks the execution as failed with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public void MarkAsFailed(string errorMessage)
    {
        Success = false;
        ErrorMessage = errorMessage;
        AddNote($"Execution failed: {errorMessage}");
    }

    /// <summary>
    /// Gets a summary description of the execution result.
    /// </summary>
    /// <returns>Human-readable execution summary.</returns>
    public string GetSummary()
    {
        if (!Success)
        {
            return $"Execution failed: {ErrorMessage}";
        }

        var actions = new List<string>();

        if (BuyOrderPlaced)
        {
            actions.Add($"Buy ${BuyAmount:N2}");
        }

        if (SellOrderPlaced)
        {
            actions.Add($"Sell {SellQuantity:N2} shares");
        }

        if (CashBufferAdjustmentExecuted)
        {
            actions.Add("Cash buffer adjusted");
        }

        if (actions.Count == 0)
        {
            return "No actions taken (conditions not met)";
        }

        return $"Actions: {string.Join(", ", actions)}. Cash ratio: {CashRatioBefore:P2} → {CashRatioAfter:P2}";
    }
}
