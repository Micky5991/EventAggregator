using System;
using System.Threading;
using FluentAssertions;
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

        called.Should().BeFalse();
        unsubscribed.Should().BeFalse();
        subscription.IsDisposed.Should().BeFalse();
        subscription.Type.Should().Be(typeof(TestEvent));
        subscription.SubscriptionOptions.IgnoreCancelled.Should().BeFalse();
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

        subscription.SubscriptionOptions.EventPriority.Should().Be(priority);
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

        subscription.SubscriptionOptions.ThreadTarget.Should().Be(threadTarget);
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

        subscription.SubscriptionOptions.IgnoreCancelled.Should().Be(ignoreCancelled);
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

        act.Should().Throw<ArgumentNullException>().WithMessage("*handler*");
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

        act.Should().Throw<ArgumentNullException>().WithMessage("*unsubscribeAction*");
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

        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [TestMethod]
    public void InvokingEventWithNullThrowsArgumentNullException()
    {
        Action act = () => _publisherThreadSubscription.Invoke(null);

        act.Should().Throw<ArgumentNullException>("*eventInstance*");
    }

    [TestMethod]
    public void DisposingSubscriptionCallsUnsubscription()
    {
        _publisherThreadSubscription.Dispose();

        _subscribeStatus.Should().BeFalse();
        _publisherThreadSubscription.IsDisposed.Should().BeTrue();
    }

    [TestMethod]
    public void DisposingSubscriptionThrowsExceptionsOnMethods()
    {
        _publisherThreadSubscription.Dispose();

        Action actDispose = () => _publisherThreadSubscription.Dispose();
        actDispose.Should().Throw<ObjectDisposedException>();

        Action actInvoke = () => _publisherThreadSubscription.Invoke(new TestEvent());
        actInvoke.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void CreatingSubscriptionWithInvalidThreadTargetThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = (ThreadTarget) int.MaxValue,
        };

        Action act = () => new Subscription<TestEvent>(
            _logger,
            e => { },
            options,
            _synchronizationContext,
            () => { });

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*threadTarget*");
    }

    [TestMethod]
    public void InvokingSubscriptionCallsHandlerWithCorreltArguments()
    {
        var eventData = new TestEvent();

        _publisherThreadSubscription.Invoke(eventData);

        _passedEvent.Should().Be(eventData);
    }

    [TestMethod]
    public void InvokingEventWithWrongInstanceThrowsArgumentException()
    {
        var eventData = new OtherTestEvent();

        Action act = () => _publisherThreadSubscription.Invoke(eventData);

        act.Should().Throw<ArgumentException>();
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

        _handleCounter.Should().Be(amount);
    }

    [TestMethod]
    public void InvokingMainThreadSubscriptionCallsSynchronizationContext()
    {
        _mainThreadSubscription.Invoke(new TestEvent());

        _synchronizationContext.InvokeAmount.Should().Be(1);
    }

    [TestMethod]
    public void InvokingPublisherThreadSubscriptionDoesNotCallSubscriptionContext()
    {
        _publisherThreadSubscription.Invoke(new TestEvent());

        _synchronizationContext.InvokeAmount.Should().Be(0);
    }

    [TestMethod]
    public void InvokingBackgroundThreadSubscriptionDoesNotCallSubscriptionContext()
    {
        _backgroundThreadSubscription.Invoke(new TestEvent());

        _synchronizationContext.InvokeAmount.Should().Be(0);
    }

    [TestMethod]
    public void ThrowingExceptionInsideHandlerCatchesException()
    {
        _handleAction = () => throw new Exception("Test");

        Action act = () => _publisherThreadSubscription.Invoke(new TestEvent());
        act.Should().NotThrow();

        Action mainAct = () => _mainThreadSubscription.Invoke(new TestEvent());
        mainAct.Should().NotThrow();

        Action backgroundAct = () => _backgroundThreadSubscription.Invoke(new TestEvent());
        backgroundAct.Should().NotThrow();
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

        eventData.Number.Should().Be(2);
        eventData.NumberChangeAmount.Should().Be(2);
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

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(IDataChangingEvent)}*");
    }

    [TestMethod]
    public void CancellableEventInterfacesImplementsRightInterfaces()
    {
        typeof(ICancellableEvent).Should().Implement<IEvent>().And.Implement<IDataChangingEvent>();
    }

    [TestMethod]
    public void DataChangingEventInterfacesImplementsRightInterfaces()
    {
        typeof(IDataChangingEvent).Should().Implement<IEvent>();
    }
}
