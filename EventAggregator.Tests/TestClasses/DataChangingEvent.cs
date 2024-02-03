using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;

namespace Micky5991.EventAggregator.Tests.TestClasses;

public class DataChangingEvent : EventBase, IDataChangingEvent
{
    private int _number;

    public int NumberChangeAmount { get; private set; }

    public int Number
    {
        get => this._number;
        set
        {
            this.NumberChangeAmount++;
            this._number = value;
        }
    }
}