using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Tests.TestClasses;

public class DataChangingEvent : EventBase, IDataChangingEvent
{
    private int number;

    public int NumberChangeAmount { get; private set; }

    public int Number
    {
        get => this.number;
        set
        {
            this.NumberChangeAmount++;
            this.number = value;
        }
    }
}