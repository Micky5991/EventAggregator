using System.Threading;

namespace Micky5991.EventAggregator.Tests.TestClasses;

public class TestSynchronizationContext : SynchronizationContext
{

    public int PostAmount { get; private set; } = 0;
    public int SendAmount { get; private set; } = 0;

    public int InvokeAmount => this.PostAmount + this.SendAmount;

    public override SynchronizationContext CreateCopy()
    {
        return new TestSynchronizationContext();
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        this.PostAmount++;
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        this.SendAmount++;
    }
}