using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Sample.Events;

public class UserPurchaseItemEvent : EventBase, IDataChangingEvent
{
    public UserPurchaseItemEvent(string username, int price, string? usedCoupon)
    {
        Username = username;
        Price = price;
        UsedCoupon = usedCoupon;
    }

    public string Username { get; }

    public int Price { get; set; }

    public string? UsedCoupon { get; }

}
