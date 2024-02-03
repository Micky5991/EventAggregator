using System;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Sample.Events;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Sample;

public class SampleService
{
    private readonly IEventAggregator _eventAggregator;

    private readonly ILogger<SampleService> _logger;

    public SampleService(IEventAggregator eventAggregator, ILogger<SampleService> logger)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    public void Initialize()
    {
        _eventAggregator.Subscribe<UserConnectedEvent>(OnUserConnected);
        _eventAggregator.Subscribe<UserConnectedEvent>(OnUserConnectedAsync);
        _eventAggregator.Subscribe<UserSendMessageEvent>(OnGuestSendsMessage);
        _eventAggregator.Subscribe<UserPurchaseItemEvent>(OnUserPurchasedItem);
        _eventAggregator.Subscribe<UserPermissionRequestEvent>(OnPermissionRequest, x => x.EventPriority = EventPriority.Lowest);
        _eventAggregator.Subscribe<UserPermissionRequestEvent>(OnModeratorPermissionRequest);
        _eventAggregator.Subscribe<UserPermissionRequestEvent>(OnSpecialUserPermissionRequest, x => x.EventPriority = EventPriority.Highest);
    }

    private async Task OnUserConnectedAsync(UserConnectedEvent eventdata)
    {
        await Task.Delay(1000);

        _logger.LogInformation("{Username} CONNECTED", eventdata.Username);

        throw new Exception("ERROR");
    }

    private void OnPermissionRequest(UserPermissionRequestEvent eventdata)
    {
        eventdata.Cancelled = true;
    }

    private void OnModeratorPermissionRequest(UserPermissionRequestEvent eventdata)
    {
        if (eventdata.Role == "Admin")
        {
            eventdata.Cancelled = false;
        }
    }

    private void OnSpecialUserPermissionRequest(UserPermissionRequestEvent eventdata)
    {
        if (eventdata.Username == "Micky5991")
        {
            eventdata.Cancelled = false;
        }
    }

    public void SendMessage(string username, string message)
    {
        var messageEvent = _eventAggregator.Publish(new UserSendMessageEvent(username, message));
        if (messageEvent.Cancelled == false)
        {
            _logger.LogInformation("{Username}: {Message}", messageEvent.Username, messageEvent.Message);
        }
    }

    public void PurchaseItem(string username, int price, string? usedCoupon = null)
    {
        var purchasedEvent = _eventAggregator.Publish(new UserPurchaseItemEvent(username, price, usedCoupon));

        _logger.LogInformation(
            "{Username} purchased item with code {UsedCoupon} for {Price} Coins",
            purchasedEvent.Username,
            purchasedEvent.UsedCoupon,
            purchasedEvent.Price);
    }

    public bool HasPermission(string username, string role)
    {
        var permissionRequest = _eventAggregator.Publish(new UserPermissionRequestEvent(username, role));

        return permissionRequest.Cancelled == false;
    }

    private void OnUserConnected(UserConnectedEvent eventData)
    {
        _logger.LogInformation("User \"{Username}\" connected", eventData.Username);
    }

    private void OnUserPurchasedItem(UserPurchaseItemEvent eventData)
    {
        if (eventData.UsedCoupon == "10OFF")
        {
            eventData.Price = (int) Math.Ceiling(eventData.Price * 0.9);
        }
    }

    private void OnGuestSendsMessage(UserSendMessageEvent eventData)
    {
        if (eventData.Username == "Guest")
        {
            eventData.Cancelled = true;
        }
    }
}
