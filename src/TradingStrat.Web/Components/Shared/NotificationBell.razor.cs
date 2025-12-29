using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Shared;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private bool _isOpen;
    private int _unreadCount;
    private bool _hasLoadedCount;

    protected override void OnInitialized()
    {
        NotificationService.OnUnreadCountChanged += HandleUnreadCountChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_hasLoadedCount)
        {
            _hasLoadedCount = true;
            _unreadCount = await NotificationService.GetUnreadCountAsync();
            StateHasChanged();
        }
    }

    private void ToggleNotificationCenter()
    {
        _isOpen = !_isOpen;
    }

    private void HandleIsOpenChanged(bool isOpen)
    {
        _isOpen = isOpen;
        StateHasChanged();
    }

    private void HandleUnreadCountChanged(int count)
    {
        InvokeAsync(() =>
        {
            _unreadCount = count;
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        NotificationService.OnUnreadCountChanged -= HandleUnreadCountChanged;
    }
}
