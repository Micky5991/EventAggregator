using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Micky5991.EventAggregator.Tests.TestClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Micky5991.EventAggregator.Tests;

[TestClass]
public class EventAggregatorFixture
{

    private ILogger<ISubscription> _subscriptionLogger;

    private TestSynchronizationContext _synchronizationContext;

    private EventAggregatorService _eventAggregator;

    [TestInitialize]
    public void Setup()
    {
        _subscriptionLogger = new NullLogger<ISubscription>();
        _synchronizationContext = new TestSynchronizationContext();

        _eventAggregator = new EventAggregatorService(_subscriptionLogger);
        _eventAggregator.SetMainThreadSynchronizationContext(_synchronizationContext);
    }

    [TestCleanup]
    public void Teardown()
    {
        _subscriptionLogger = null;
        _synchronizationContext = null;

        _eventAggregator = null;
    }

    [TestMethod]
    public void BuildEventAggregatorWorks()
    {
        var aggregator = new EventAggregatorService(_subscriptionLogger);
    }

    [TestMethod]
    public void BuildEventAggregatorWithSubscriptionLoggerNullThrowsException()
    {
        Action act = () => new EventAggregatorService(null);

        act.ShouldThrow<ArgumentNullException>();
    }

    [TestMethod]
    public void SubscribeEventAggregatorWithoutSynchronizationContextThrowsException()
    {
        var aggregator = new EventAggregatorService(_subscriptionLogger);

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.MainThread,
        };

        Action act = () => aggregator.Subscribe<TestEvent>(x => {}, options);

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(SynchronizationContext));
    }

    [TestMethod]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.PublisherThread)]
    public void SubscribeEventWithTargetThatDoesntUseMainThreadContextWorks(ThreadTarget threadTarget)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = threadTarget,
        };

        var subscription = new EventAggregatorService(_subscriptionLogger)
            .Subscribe<TestEvent>(e => {}, options);

        subscription.ShouldNotBeNull();
    }

    [TestMethod]
    public void SubscribeEventWithTargetThatUsesMainThreadContextThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.MainThread,
        };

        Action act = () => new EventAggregatorService(_subscriptionLogger).Subscribe<TestEvent>(e => {}, options);

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(SynchronizationContext));
    }

    [TestMethod]
    public void SubscribeEventWithUnknownEventPriorityThrowsException()
    {
        Action act = () => _eventAggregator.Subscribe<TestEvent>(e => { }, new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = (EventPriority) int.MaxValue,
            ThreadTarget = ThreadTarget.PublisherThread,
        });

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void SubscribeEventWithUnknownThreadTargetThrowsException()
    {
        Action act = () => _eventAggregator.Subscribe<TestEvent>(e => { }, new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = (ThreadTarget) int.MaxValue,
        });

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void SubscribeWithNullAsHandlerThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        Action act = () => _eventAggregator.Subscribe<TestEvent>((IEventAggregator.EventHandlerDelegate<TestEvent>)null, options);

        act.ShouldThrow<ArgumentNullException>();
    }

    [TestMethod]
    public void SubscribeWithNullAsAsyncHandlerThrowsException()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        Action act = () => _eventAggregator.Subscribe<TestEvent>((IEventAggregator.AsyncEventHandlerDelegate<TestEvent>)null, options);

        act.ShouldThrow<ArgumentNullException>();
    }

    [TestMethod]
    [DataRow(EventPriority.Lowest)]
    [DataRow(EventPriority.Low)]
    [DataRow(EventPriority.Normal)]
    [DataRow(EventPriority.High)]
    [DataRow(EventPriority.Highest)]
    [DataRow(EventPriority.Monitor)]
    public void EventPriorityWillBeSetInSubscription(EventPriority eventPriority)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = eventPriority,
            ThreadTarget = ThreadTarget.MainThread,
        };

        var subscription = _eventAggregator.Subscribe<TestEvent>(e => { }, options);

        subscription.ShouldNotBeNull();
        subscription.SubscriptionOptions.EventPriority.ShouldBe(eventPriority);
    }

    [TestMethod]
    [DataRow(ThreadTarget.MainThread)]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.PublisherThread)]
    public void ThreadTargetWillBeSetInSubscription(ThreadTarget threadTarget)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = threadTarget,
        };

        var subscription = _eventAggregator.Subscribe<TestEvent>(e => { }, options);

        subscription.ShouldNotBeNull();
        subscription.SubscriptionOptions.ThreadTarget.ShouldBe(threadTarget);
    }

    [TestMethod]
    public void PublishingNullThrowsArgumentNullException()
    {
        Action act = () => _eventAggregator.Publish<TestEvent>(null);

        act.ShouldThrow<ArgumentNullException>();
    }

    [TestMethod]
    public void PublishingEventPassesCorrectInstance()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        TestEvent receivedEvent = null;
        var sentEvent = new TestEvent();

        _eventAggregator.Subscribe<TestEvent>(e => receivedEvent = e, options);

        _eventAggregator.Publish(sentEvent);

        receivedEvent.ShouldBeSameAs(sentEvent);
    }

    [TestMethod]
    public void SubscribeMultipleTimesToEventInvokesMultipleHandlers()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var calls = new List<int>();

        _eventAggregator.Subscribe<TestEvent>(e => calls.Add(1), options);

        _eventAggregator.Subscribe<TestEvent>(e => calls.Add(2), options);

        _eventAggregator.Subscribe<TestEvent>(e => calls.Add(3), options);

        _eventAggregator.Publish(new TestEvent());

        calls.Count.ShouldBe(3);
        calls.ShouldContain(1);
        calls.ShouldContain(2);
        calls.ShouldContain(3);
    }

    [TestMethod]
    public void EventPriorityWillBeInvokedInCorrectOrder()
    {
        var calls = new List<int>();

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(1), x => x.EventPriority = EventPriority.Lowest);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(2), x => x.EventPriority = EventPriority.Low);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(3), x => x.EventPriority = EventPriority.Normal);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(4), x => x.EventPriority = EventPriority.High);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(5), x => x.EventPriority = EventPriority.Highest);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(6), x => x.EventPriority = EventPriority.Monitor);

        _eventAggregator.Publish(new TestEvent());

        calls.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 }, ignoreOrder: false);
    }

    [TestMethod]
    public void ThrowingExceptionInsideHandlerExecutesAllOtherHandlers()
    {
        var calls = new List<int>();

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(1), x => x.EventPriority = EventPriority.Lowest);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(2), x => x.EventPriority = EventPriority.Low);

        _eventAggregator
            .Subscribe<TestEvent>(e => throw new Exception("OMEGALUL"), x => x.EventPriority = EventPriority.Normal);

        _eventAggregator
            .Subscribe<TestEvent>(e => calls.Add(3), x => x.EventPriority = EventPriority.High);

        Action act = () => _eventAggregator.Publish(new TestEvent());

        act.ShouldNotThrow();

        calls.ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: false);
    }

    [TestMethod]
    public void PublishingOtherEventDoesNothing()
    {
        Action act = () => _eventAggregator.Publish(new OtherTestEvent());

        act.ShouldNotThrow();
    }

    [TestMethod]
    public void PublishingEventOnlyExecutesGivenEventHandlers()
    {
        var wrongCalled = false;
        var rightCalled = false;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<TestEvent>(_ => wrongCalled = true, options);
        _eventAggregator.Subscribe<OtherTestEvent>(_ => rightCalled = true, options);

        _eventAggregator.Publish(new OtherTestEvent());

        wrongCalled.ShouldBeFalse();
        rightCalled.ShouldBeTrue();
    }

    [TestMethod]
    public void UnsubscribingDisposesSubscription()
    {
        var subscription = Substitute.For<ISubscription>();

        _eventAggregator.Unsubscribe(subscription);

        subscription.Received(1).Dispose();
    }

    [TestMethod]
    public void PassingNullToUnsubscribeThrowsException()
    {
        Action act = () => _eventAggregator.Unsubscribe(null);

        act.ShouldThrow<ArgumentNullException>();
    }

    [TestMethod]
    public void DisposingCreatedSubscriptionUnsubscribesFromAggregator()
    {
        var calledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = _eventAggregator.Subscribe<TestEvent>(_ => calledAmount++, options);

        _eventAggregator.Publish(new TestEvent());

        subscription.Dispose();

        _eventAggregator.Publish(new TestEvent());

        subscription.IsDisposed.ShouldBeTrue();
        calledAmount.ShouldBe(1);
    }

    [TestMethod]
    public void PublishingEventWithNoIgnoreCancelledWillBeExecuted()
    {
        var calledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<CancellableEvent>(_ => calledAmount++, options);

        _eventAggregator.Publish(new CancellableEvent());

        calledAmount.ShouldBe(1);
    }

    [TestMethod]
    public void PublishingCancellableEventAndIgnoreWithoutActualCancellingWillStillExecute()
    {
        var calledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = true,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<CancellableEvent>(_ => calledAmount++, options);

        _eventAggregator.Publish(new CancellableEvent());

        calledAmount.ShouldBe(1);
    }

    [TestMethod]
    public void PublishingCancelledCancellableEventWillIgnoreHandlersThatMarkedSo()
    {
        var wrongCalledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Low,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<CancellableEvent>(e => e.Cancelled = true, options);


        var secondOptions = new SubscriptionOptions
        {
            IgnoreCancelled = true,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<CancellableEvent>(e => wrongCalledAmount++, secondOptions);

        var cancelledEvent = _eventAggregator.Publish(new CancellableEvent());

        cancelledEvent.Cancelled.ShouldBeTrue();

        wrongCalledAmount.ShouldBe(0);
    }

    [TestMethod]
    public void HigherEventPriorityCancellableEventWillHaveNoEffectOnLowerPriority()
    {
        var correctCalledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.High,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<CancellableEvent>(e => e.Cancelled = true, options);

        var secondOptions = new SubscriptionOptions
        {
            IgnoreCancelled = true,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<CancellableEvent>(e => correctCalledAmount++, secondOptions);

        var cancelledEvent = _eventAggregator.Publish(new CancellableEvent());

        cancelledEvent.Cancelled.ShouldBeTrue();

        correctCalledAmount.ShouldBe(1);
    }

    [TestMethod]
    public void RemovingSubscriptionDuringEventHandlerWillSkipEvent()
    {
        var wrongCalledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.High,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = _eventAggregator.Subscribe<TestEvent>(e => wrongCalledAmount++, options);

        var secondOptions = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<TestEvent>(e => subscription.Dispose(), secondOptions);

        _eventAggregator.Publish(new TestEvent());

        wrongCalledAmount.ShouldBe(0);
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public void DisposingEventInsideSameHandlerRemovesForFurtherPublishes()
    {
        var wasDisposed = false;
        var calledAmount = 0;
        ISubscription subscription = null;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        subscription = _eventAggregator.Subscribe<TestEvent>(
            e =>
            {
                calledAmount++;

                if (subscription != null)
                {
                    wasDisposed = true;
                    subscription.Dispose();
                }
            },
            options);

        _eventAggregator.Publish(new TestEvent());
        _eventAggregator.Publish(new TestEvent());

        calledAmount.ShouldBe(1);
        wasDisposed.ShouldBeTrue();
    }

    [TestMethod]
    public void SubscribingToEventInsideHandlerWillPublishForNextEvent()
    {
        var calledAmount = 0;
        var innerCalledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<TestEvent>(
            e =>
            {
                calledAmount++;

                _eventAggregator.Subscribe<TestEvent>(i => innerCalledAmount++, options);
            },
            options);

        _eventAggregator.Publish(new TestEvent());
        _eventAggregator.Publish(new TestEvent());

        calledAmount.ShouldBe(2);
        innerCalledAmount.ShouldBe(1);
    }

    [TestMethod]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.MainThread)]
    public void SubscribingToDataChangingEventInWrongThreadTargetThrowsException(ThreadTarget threadTarget)
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = threadTarget,
        };

        Action act = () => _eventAggregator.Subscribe<DataChangingEvent>(_ => { }, options);

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(IDataChangingEvent));
    }

    [TestMethod]
    public void PublishEventWithDifferentCompileTimeTypeDispatchesRightSubscription()
    {
        IEvent eventData = new TestEvent();
        var calledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<TestEvent>(e => calledAmount++, options);

        _eventAggregator.Publish(eventData);

        calledAmount.ShouldBe(1);
    }

    [TestMethod]
    public void SubscribingToAsyncEventWillReturnCorrectly()
    {
        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        var subscription = _eventAggregator.Subscribe<TestEvent>(_ => Task.CompletedTask, options);

        subscription.IsDisposed.ShouldBeFalse();
        subscription.SubscriptionOptions.EventPriority.ShouldBe(EventPriority.Normal);
        subscription.SubscriptionOptions.IgnoreCancelled.ShouldBeFalse();
        subscription.SubscriptionOptions.ThreadTarget.ShouldBe(ThreadTarget.PublisherThread);

        subscription.ShouldNotBeNull();
        subscription.ShouldBeAssignableTo<ISubscription>();
    }

    [TestMethod]
    public async Task AsyncSubscriptionHandlerWillBeTriggered()
    {
        var calledAmount = 0;

        var options = new SubscriptionOptions
        {
            IgnoreCancelled = false,
            EventPriority = EventPriority.Normal,
            ThreadTarget = ThreadTarget.PublisherThread,
        };

        _eventAggregator.Subscribe<TestEvent>(
            _ =>
            {
                calledAmount++;

                return Task.CompletedTask;
            },
            options);

        _eventAggregator.Publish(new TestEvent());

        await Task.Delay(TimeSpan.FromSeconds(1));

        calledAmount.ShouldBe(1);
    }
}
