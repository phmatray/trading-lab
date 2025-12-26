using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Generic table component with multi-select functionality.
/// </summary>
/// <typeparam name="TItem">Type of items displayed in the table.</typeparam>
public partial class MultiSelectTable<TItem> : ComponentBase
{
    #region Parameters

    /// <summary>
    /// The items to display in the table.
    /// </summary>
    [Parameter, EditorRequired]
    public required IEnumerable<TItem> Items { get; set; }

    /// <summary>
    /// Set of keys for selected items.
    /// </summary>
    [Parameter]
    public HashSet<string> SelectedKeys { get; set; } = new();

    /// <summary>
    /// Function to extract the unique key from an item.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TItem, string> GetItemKey { get; set; }

    /// <summary>
    /// Whether to show the "Select All" checkbox in the header.
    /// </summary>
    [Parameter]
    public bool AllowSelectAll { get; set; } = true;

    /// <summary>
    /// Template for table header columns (excluding checkbox column).
    /// </summary>
    [Parameter, EditorRequired]
    public required RenderFragment HeaderContent { get; set; }

    /// <summary>
    /// Template for each row's content (excluding checkbox column).
    /// </summary>
    [Parameter, EditorRequired]
    public required RenderFragment<TItem> RowContent { get; set; }

    /// <summary>
    /// Message to display when there are no items.
    /// </summary>
    [Parameter]
    public string EmptyMessage { get; set; } = "No items to display";

    /// <summary>
    /// Callback invoked when selection changes.
    /// </summary>
    [Parameter]
    public EventCallback<HashSet<string>> OnSelectionChanged { get; set; }

    #endregion

    #region Properties

    private bool IsAllSelected =>
        Items.Any() && Items.All(item => SelectedKeys.Contains(GetItemKey(item)));

    #endregion

    #region Helper Methods

    private bool IsSelected(TItem item) =>
        SelectedKeys.Contains(GetItemKey(item));

    #endregion

    #region Event Handlers

    private async Task HandleSelectAllChanged(ChangeEventArgs e)
    {
        bool isChecked = (bool)(e.Value ?? false);

        if (isChecked)
        {
            // Select all
            foreach (TItem item in Items)
            {
                SelectedKeys.Add(GetItemKey(item));
            }
        }
        else
        {
            // Deselect all
            SelectedKeys.Clear();
        }

        await OnSelectionChanged.InvokeAsync(SelectedKeys);
    }

    private async Task HandleRowSelectionChanged(TItem item, ChangeEventArgs e)
    {
        bool isChecked = (bool)(e.Value ?? false);
        string key = GetItemKey(item);

        if (isChecked)
        {
            SelectedKeys.Add(key);
        }
        else
        {
            SelectedKeys.Remove(key);
        }

        await OnSelectionChanged.InvokeAsync(SelectedKeys);
    }

    #endregion
}
