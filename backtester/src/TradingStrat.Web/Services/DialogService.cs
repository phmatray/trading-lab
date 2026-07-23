using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Services;

/// <summary>
/// Service for managing dialogs programmatically.
/// </summary>
public class DialogService
{
    private readonly List<DialogInstance> _dialogs = [];

    /// <summary>
    /// Event raised when dialogs change.
    /// </summary>
    public event EventHandler? OnChange;

    /// <summary>
    /// Get all active dialogs.
    /// </summary>
    public IReadOnlyList<DialogInstance> ActiveDialogs => _dialogs.AsReadOnly();

    /// <summary>
    /// Show a confirmation dialog.
    /// </summary>
    public Guid ShowConfirm(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        var id = Guid.NewGuid();
        var dialog = new DialogInstance(
            id,
            title,
            message,
            DialogType.Confirm,
            confirmText,
            cancelText
        );

        _dialogs.Add(dialog);
        OnChange?.Invoke(this, EventArgs.Empty);
        return id;
    }

    /// <summary>
    /// Show an alert dialog.
    /// </summary>
    public Guid ShowAlert(string title, string message, string closeText = "OK")
    {
        var id = Guid.NewGuid();
        var dialog = new DialogInstance(
            id,
            title,
            message,
            DialogType.Alert,
            closeText,
            null
        );

        _dialogs.Add(dialog);
        OnChange?.Invoke(this, EventArgs.Empty);
        return id;
    }

    /// <summary>
    /// Show a custom dialog with render fragments.
    /// </summary>
    public Guid ShowCustom(string title, RenderFragment body, RenderFragment? actions = null)
    {
        var id = Guid.NewGuid();
        var dialog = new DialogInstance(
            id,
            title,
            body,
            actions,
            DialogType.Custom
        );

        _dialogs.Add(dialog);
        OnChange?.Invoke(this, EventArgs.Empty);
        return id;
    }

    /// <summary>
    /// Close a dialog by ID.
    /// </summary>
    public void Close(Guid id)
    {
        DialogInstance? dialog = _dialogs.FirstOrDefault(d => d.Id == id);
        if (dialog != null)
        {
            _dialogs.Remove(dialog);
            OnChange?.Invoke(this, EventArgs.Empty);

            // Invoke callback if exists
            dialog.OnClose?.Invoke();
        }
    }

    /// <summary>
    /// Confirm a dialog (for confirmation dialogs).
    /// </summary>
    public void Confirm(Guid id)
    {
        DialogInstance? dialog = _dialogs.FirstOrDefault(d => d.Id == id);
        if (dialog != null)
        {
            _dialogs.Remove(dialog);
            OnChange?.Invoke(this, EventArgs.Empty);

            // Invoke confirm callback if exists
            dialog.OnConfirm?.Invoke();
        }
    }

    /// <summary>
    /// Close all dialogs.
    /// </summary>
    public void CloseAll()
    {
        _dialogs.Clear();
        OnChange?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Represents a dialog instance.
/// </summary>
public class DialogInstance
{
    public Guid Id { get; }
    public string Title { get; }
    public string? Message { get; }
    public RenderFragment? Body { get; }
    public RenderFragment? Actions { get; }
    public DialogType Type { get; }
    public string? ConfirmText { get; }
    public string? CancelText { get; }
    public Action? OnConfirm { get; set; }
    public Action? OnClose { get; set; }

    // Constructor for simple dialogs (alert/confirm)
    public DialogInstance(
        Guid id,
        string title,
        string message,
        DialogType type,
        string? confirmText,
        string? cancelText)
    {
        Id = id;
        Title = title;
        Message = message;
        Type = type;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }

    // Constructor for custom dialogs
    public DialogInstance(
        Guid id,
        string title,
        RenderFragment body,
        RenderFragment? actions,
        DialogType type)
    {
        Id = id;
        Title = title;
        Body = body;
        Actions = actions;
        Type = type;
    }
}

/// <summary>
/// Dialog types.
/// </summary>
public enum DialogType
{
    Alert,
    Confirm,
    Custom
}
