namespace TradingStrat.Web.Models.State;

public class FormStateContainer
{
    public Dictionary<string, string> SavedForms { get; set; } = new();
    public DateTime LastCleanup { get; set; } = DateTime.UtcNow;
}
