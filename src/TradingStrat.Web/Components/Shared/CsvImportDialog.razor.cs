using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Dialog for importing ticker symbols from CSV files.
/// </summary>
public partial class CsvImportDialog : ComponentBase
{
    #region Parameters

    /// <summary>
    /// Whether the dialog is visible.
    /// </summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Callback invoked when tickers are successfully imported.
    /// </summary>
    [Parameter]
    public EventCallback<List<string>> OnTickersImported { get; set; }

    /// <summary>
    /// Callback invoked when the dialog is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    #endregion

    #region Private Fields

    private List<string> _previewTickers = new();
    private string? _errorMessage;

    #endregion

    #region Event Handlers

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        _errorMessage = null;
        _previewTickers.Clear();

        try
        {
            IBrowserFile file = e.File;

            // Validate file size (max 1MB)
            if (file.Size > 1024 * 1024)
            {
                _errorMessage = "File size exceeds 1MB limit.";
                return;
            }

            // Read file content
            using Stream stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024);
            using StreamReader reader = new(stream);
            string content = await reader.ReadToEndAsync();

            // Parse tickers
            _previewTickers = ParseTickers(content);

            if (!_previewTickers.Any())
            {
                _errorMessage = "No valid tickers found in the file.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error reading file: {ex.Message}";
        }
    }

    private async Task HandleImport()
    {
        if (_previewTickers.Any())
        {
            await OnTickersImported.InvokeAsync(_previewTickers);
            await HandleClose();
        }
    }

    private async Task HandleClose()
    {
        _previewTickers.Clear();
        _errorMessage = null;
        await OnClose.InvokeAsync();
    }

    #endregion

    #region Helper Methods

    private List<string> ParseTickers(string content)
    {
        HashSet<string> tickers = new(StringComparer.OrdinalIgnoreCase);

        // Split by newlines and commas
        string[] lines = content.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string ticker = line.Trim().ToUpperInvariant();

            // Basic validation: 1-5 uppercase letters/numbers
            if (!string.IsNullOrEmpty(ticker) &&
                ticker.Length >= 1 &&
                ticker.Length <= 10 &&
                ticker.All(c => char.IsLetterOrDigit(c) || c == '.'))
            {
                tickers.Add(ticker);
            }
        }

        return tickers.OrderBy(t => t).ToList();
    }

    #endregion
}
