using System;
using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Sample.Events;
using Microsoft.Extensions.Logging;

namespace Micky5991.EventAggregator.Sample;

public class SampleService
{
    private readonly IEventAggregator eventAggregator;

    private readonly ILogger<SampleService> logger;

    public SampleService(IEventAggregator eventAggregator, ILogger<SampleService> logger)
    {
            this.eventAggregator = eventAggregator;
            this.logger = logger;
        }

    public void Initialize()
    {
            this.eventAggregator.Subscribe<UserConnectedEvent>(this.OnUserConnected);
            this.eventAggregator.Subscribe<UserConnectedEvent>(this.OnUserConnectedAsync);
            this.eventAggregator.Subscribe<UserSendMessageEvent>(this.OnGuestSendsMessage);
            this.eventAggregator.Subscribe<UserPurchaseItemEvent>(this.OnUserPurchasedItem);
            this.eventAggregator.Subscribe<UserPermissionRequestEvent>(this.OnPermissionRequest, eventPriority: EventPriority.Lowest);
            this.eventAggregator.Subscribe<UserPermissionRequestEvent>(this.OnModeratorPermissionRequest);
            this.eventAggregator.Subscribe<UserPermissionRequestEvent>(this.OnSpecialUserPermissionRequest, eventPriority: EventPriority.Highest);
        }

    private async Task OnUserConnectedAsync(UserConnectedEvent eventdata)
    {
            await Task.Delay(1000);

            this.logger.LogInformation("{Username} CONNECTED", eventdata.Username);

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
            var messageEvent = this.eventAggregator.Publish(new UserSendMessageEvent(username, message));
            if (messageEvent.Cancelled == false)
            {
                this.logger.LogInformation("{Username}: {Message}", messageEvent.Username, messageEvent.Message);
            }
        }

    public void PurchaseItem(string username, int price, string? usedCoupon = null)
    {
            var purchasedEvent = this.eventAggregator.Publish(new UserPurchaseItemEvent(username, price, usedCoupon));

            this.logger.LogInformation(
                                       "{Username} purchased item with code {UsedCoupon} for {Price} Coins",
                                       purchasedEvent.Username,
                                       purchasedEvent.UsedCoupon,
                                       purchasedEvent.Price);
        }

    public bool HasPermission(string username, string role)
    {
            var permissionRequest = this.eventAggregator.Publish(new UserPermissionRequestEvent(username, role));

            return permissionRequest.Cancelled == false;
        }

    private void OnUserConnected(UserConnectedEvent eventData)
    {
            this.logger.LogInformation("User \"{Username}\" connected", eventData.Username);
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