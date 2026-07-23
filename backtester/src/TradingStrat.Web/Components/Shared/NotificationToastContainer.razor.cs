using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Shared;

public partial class NotificationToastContainer : ComponentBase, IDisposable
{
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private const int MaxVisibleToasts = 3;
    private readonly List<Notification> _activeToasts = new();

    protected override void OnInitialized()
    {
        NotificationService.OnNotificationAdded += HandleNotificationAdded;
    }

    private void HandleNotificationAdded(Notification notification)
    {
        InvokeAsync(() =>
        {
            _activeToasts.Insert(0, notification);

            if (_activeToasts.Count > MaxVisibleToasts)
            {
                _activeToasts.RemoveAt(_activeToasts.Count - 1);
            }

            StateHasChanged();
        });
    }

    private void RemoveToast(Notification notification)
    {
        InvokeAsync(() =>
        {
            _activeToasts.Remove(notification);
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        NotificationService.OnNotificationAdded -= HandleNotificationAdded;
    }
}
