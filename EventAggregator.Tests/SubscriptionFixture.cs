using System;
using System.Threading;
using Shouldly;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Tests.TestClasses;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ICancellableEvent = Micky5991.EventAggregator.Interfaces.ICancellableEvent;

namespace Micky5991.EventAggregator.Tests;

[TestClass]
public class SubscriptionFixture
{
    private IEvent _passedEvent;

    private int _handleCounter;

    private bool _subscribeStatus = true;

    private NullLogger<ISubscription> _logger;

    private Action _handleAction;

    private TestSynchronizationContext _synchronizationContext;

    private Subscription<TestEvent> _publisherThreadSubscription;

    private Subscription<TestEvent> _mainThreadSubscription;

    private Subscription<TestEvent> _backgroundThreadSubscription;

    [TestInitialize]
    public void Setup()
    {
        _synchronizationContext = new TestSynchronizationContext();
        _handleAction = () => { };
        _logger = new NullLogger<ISubscription>();

        SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _publisherThreadSubscription = new Subscription<TestEvent>(
            _logger,
            e =>
            {
                _passedEvent = e;
                _handleCounter++;

                _handleAction();
            },
            options,
            _synchronizationContext,
            () => _subscribeStatus = false);

        var secondOptions = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.MainThread,
        };

        _mainThreadSubscription = new Subscription<TestEvent>(
            _logger,
            e =>
            {
                _passedEvent = e;
                _handleCounter++;

                _handleAction();
            },
            secondOptions,
            _synchronizationContext,
            () => _subscribeStatus = false);

        var thirdOptions = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.BackgroundThread,
        };

        _backgroundThreadSubscription = new Subscription<TestEvent>(
            _logger,
            e =>
            {
                _passedEvent = e;
                _handleCounter++;

                _handleAction();
            },
            thirdOptions,
            _synchronizationContext,
            () => _subscribeStatus = false);
    }

    [TestCleanup]
    public void Teardown()
    {
        _handleCounter = 0;
        _subscribeStatus = true;
        _publisherThreadSubscription = null;
        _passedEvent = null;
        _synchronizationContext = null;

        SynchronizationContext.SetSynchronizationContext(null);
    }

    [TestMethod]
    public void CreationOfSubscriptionShouldWork()
    {
        var called = false;
        var unsubscribed = false;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = new Subscription<TestEvent>(
            _logger,
            e => called = true,
            options,
            _synchronizationContext,
            () => unsubscribed = true);

        called.ShouldBeFalse();
        unsubscribed.ShouldBeFalse();
        subscription.IsDisposed.ShouldBeFalse();
        subscription.Type.ShouldBe(typeof(TestEvent));
        subscription.SubscriptionOptions.IgnoreCancelled.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(EventPriority.Lowest)]
    [DataRow(EventPriority.Low)]
    [DataRow(EventPriority.Normal)]
    [DataRow(EventPriority.High)]
    [DataRow(EventPriority.Highest)]
    [DataRow(EventPriority.Monitor)]
    public void SubscriptionPriorityWillBeSet(EventPriority priority)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = priority,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = new Subscription<TestEvent>(
            _logger,
            e => {},
            options,
            _synchronizationContext,
            () => {});

        subscription.SubscriptionOptions.EventPriority.ShouldBe(priority);
    }

    [TestMethod]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.MainThread)]
    [DataRow(ThreadTarget.PublisherThread)]
    public void SubscriptionThreadTargetWillBeSet(ThreadTarget threadTarget)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = threadTarget,
        };

        var subscription = new Subscription<TestEvent>(
            _logger,
            e => {},
            options,
            _synchronizationContext,
            () => {});

        subscription.SubscriptionOptions.ThreadTarget.ShouldBe(threadTarget);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SubscriptionIgnoreCancelledWillBeSet(bool ignoreCancelled)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = ignoreCancelled,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = new Subscription<TestEvent>(
            _logger,
            e => {},
            options,
            _synchronizationContext,
            () => {});

        subscription.SubscriptionOptions.IgnoreCancelled.ShouldBe(ignoreCancelled);
    }

    [TestMethod]
    public void CreationOfSubscriptionWithNullHandlerThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        Action act = () => new Subscription<TestEvent>(
            _logger,
            null,
            options,
            _synchronizationContext,
            () => { });

        act.ShouldThrow<ArgumentNullException>().Message.ShouldContain("handler");
    }

    [TestMethod]
    public void CreationOfSubscriptionWithNullUnsubscriberThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        Action act = () => new Subscription<TestEvent>(
            _logger,
            e => { },
            options,
            _synchronizationContext,
            null);

        act.ShouldThrow<ArgumentNullException>().Message.ShouldContain("unsubscribeAction");
    }

    [TestMethod]
    public void CreationOfSubscriptionWithNullLoggerThrowsThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        Action act = () => new Subscription<TestEvent>(
            null,
            e => { },
            options,
            _synchronizationContext,
            () => { });

        act.ShouldThrow<ArgumentNullException>().Message.ShouldContain("logger");
    }

    [TestMethod]
    public void InvokingEventWithNullThrowsArgumentNullException()
    {
        var act = () => _publisherThreadSubscription.Invoke(null);

        act.ShouldThrow<ArgumentNullException>().Message.ShouldContain("eventInstance");
    }

    [TestMethod]
    public void DisposingSubscriptionCallsUnsubscription()
    {
        _publisherThreadSubscription.Dispose();

        _subscribeStatus.ShouldBeFalse();
        _publisherThreadSubscription.IsDisposed.ShouldBeTrue();
    }

    [TestMethod]
    public void DisposingSubscriptionThrowsExceptionsOnMethods()
    {
        _publisherThreadSubscription.Dispose();

        var actDispose = () => _publisherThreadSubscription.Dispose();
        actDispose.ShouldThrow<ObjectDisposedException>();

        var actInvoke = () => _publisherThreadSubscription.Invoke(new TestEvent());
        actInvoke.ShouldThrow<ObjectDisposedException>();
    }

    [TestMethod]
    public void CreatingSubscriptionWithInvalidThreadTargetThrowsException()
    {
        Action act = () => new Subscription<TestEvent>(
            _logger,
            e => { },
            new SubscriptionOptions
            {
                IgnoreCancelled = false,
                EventPriority = EventPriority.Normal,
                ThreadTarget = (ThreadTarget) int.MaxValue,
            },
            _synchronizationContext,
            () => { });

        act.ShouldThrow<ArgumentOutOfRangeException>().Message.ShouldContain("threadTarget");
    }

    [TestMethod]
    public void InvokingSubscriptionCallsHandlerWithCorreltArguments()
    {
        var eventData = new TestEvent();

        _publisherThreadSubscription.Invoke(eventData);

        _passedEvent.ShouldBe(eventData);
    }

    [TestMethod]
    public void InvokingEventWithWrongInstanceThrowsArgumentException()
    {
        var eventData = new OtherTestEvent();

        var act = () => _publisherThreadSubscription.Invoke(eventData);

        act.ShouldThrow<ArgumentException>();
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(10)]
    [DataRow(100)]
    public void InvokingSubscriptionMultipleTimesCallsTheseAmount(int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            _publisherThreadSubscription.Invoke(new TestEvent());
        }

        _handleCounter.ShouldBe(amount);
    }

    [TestMethod]
    public void InvokingMainThreadSubscriptionCallsSynchronizationContext()
    {
        _mainThreadSubscription.Invoke(new TestEvent());

        _synchronizationContext.InvokeAmount.ShouldBe(1);
    }

    [TestMethod]
    public void InvokingPublisherThreadSubscriptionDoesNotCallSubscriptionContext()
    {
        _publisherThreadSubscription.Invoke(new TestEvent());

        _synchronizationContext.InvokeAmount.ShouldBe(0);
    }

    [TestMethod]
    public void InvokingBackgroundThreadSubscriptionDoesNotCallSubscriptionContext()
    {
        _backgroundThreadSubscription.Invoke(new TestEvent());

        _synchronizationContext.InvokeAmount.ShouldBe(0);
    }

    [TestMethod]
    public void ThrowingExceptionInsideHandlerCatchesException()
    {
        _handleAction = () => throw new Exception("Test");

        var act = () => _publisherThreadSubscription.Invoke(new TestEvent());
        act.ShouldNotThrow();

        var mainAct = () => _mainThreadSubscription.Invoke(new TestEvent());
        mainAct.ShouldNotThrow();

        var backgroundAct = () => _backgroundThreadSubscription.Invoke(new TestEvent());
        backgroundAct.ShouldNotThrow();
    }

    [TestMethod]
    public void InvokingDataChangingEventWillKeepInstanceSame()
    {
        var eventData = new DataChangingEvent
        {
            Number = 5,
        };

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = new Subscription<DataChangingEvent>(
            _logger,
            e => { e.Number = 2; },
            options,
            _synchronizationContext,
            () => { });

        subscription.Invoke(eventData);

        eventData.Number.ShouldBe(2);
        eventData.NumberChangeAmount.ShouldBe(2);
    }

    [TestMethod]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.MainThread)]
    public void SubscribingToDataChangingEventInNonPublishThreadThrowsException(ThreadTarget threadTarget)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = threadTarget,
        };

        Action act = () => new Subscription<DataChangingEvent>(
            _logger,
            _ => { },
            options,
            _synchronizationContext,
            () => { });

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(IDataChangingEvent));
    }

    [TestMethod]
    public void CancellableEventInterfacesImplementsRightInterfaces()
    {
        typeof(IEvent).IsAssignableFrom(typeof(ICancellableEvent)).ShouldBeTrue();
        typeof(IDataChangingEvent).IsAssignableFrom(typeof(ICancellableEvent)).ShouldBeTrue();
    }

    [TestMethod]
    public void DataChangingEventInterfacesImplementsRightInterfaces()
    {
        typeof(IEvent).IsAssignableFrom(typeof(IDataChangingEvent)).ShouldBeTrue();
    }
}
