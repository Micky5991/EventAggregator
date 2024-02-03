using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
            this._subscriptionLogger = new NullLogger<ISubscription>();
            this._synchronizationContext = new TestSynchronizationContext();

            this._eventAggregator = new EventAggregatorService(this._subscriptionLogger);
            this._eventAggregator.SetMainThreadSynchronizationContext(this._synchronizationContext);
        }

    [TestCleanup]
    public void Teardown()
    {
            this._subscriptionLogger = null;
            this._synchronizationContext = null;

            this._eventAggregator = null;
        }

    [TestMethod]
    public void BuildEventAggregatorWorks()
    {
            var aggregator = new EventAggregatorService(this._subscriptionLogger);
        }

    [TestMethod]
    public void BuildEventAggregatorWithSubscriptionLoggerNullThrowsException()
    {
            Action act = () => new EventAggregatorService(null);

            act.Should().Throw<ArgumentNullException>();
        }

    [TestMethod]
    public void SubscribeEventAggregatorWithoutSynchronizationContextThrowsException()
    {
            var aggregator = new EventAggregatorService(this._subscriptionLogger);

            Action act = () => aggregator.Subscribe<TestEvent>(
                                                               x => {},
                                                               false,
                                                               EventPriority.Normal,
                                                               ThreadTarget.MainThread);

            act.Should()
               .Throw<InvalidOperationException>().WithMessage($"*{nameof(SynchronizationContext)}*");
        }

    [TestMethod]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.PublisherThread)]
    public void SubscribeEventWithTargetThatDoesntUseMainThreadContextWorks(ThreadTarget threadTarget)
    {
            var subscription = new EventAggregatorService(this._subscriptionLogger)
                                   .Subscribe<TestEvent>(e => {}, false, EventPriority.Normal, threadTarget);

            subscription.Should().NotBeNull();
        }

    [TestMethod]
    public void SubscribeEventWithTargetThatUsesMainThreadContextThrowsException()
    {
            Action act = () => new EventAggregatorService(this._subscriptionLogger)
                                   .Subscribe<TestEvent>(e => {}, false, EventPriority.Normal, ThreadTarget.MainThread);

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(SynchronizationContext)}*");
        }

    [TestMethod]
    public void SubscribeEventWithUnknownEventPriorityThrowsException()
    {
            Action act = () => this._eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, (EventPriority) int.MaxValue, ThreadTarget.PublisherThread);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

    [TestMethod]
    public void SubscribeEventWithUnknownThreadTargetThrowsException()
    {
            Action act = () => this._eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, EventPriority.Normal, (ThreadTarget) int.MaxValue);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

    [TestMethod]
    public void SubscribeWithNullAsHandlerThrowsException()
    {
            Action act = () => this._eventAggregator
                                   .Subscribe<TestEvent>((IEventAggregator.EventHandlerDelegate<TestEvent>)null, false, EventPriority.Normal, ThreadTarget.PublisherThread);

            act.Should().Throw<ArgumentNullException>();
        }

    [TestMethod]
    public void SubscribeWithNullAsAsyncHandlerThrowsException()
    {
            Action act = () => this._eventAggregator
                                   .Subscribe<TestEvent>((IEventAggregator.AsyncEventHandlerDelegate<TestEvent>)null, false, EventPriority.Normal, ThreadTarget.PublisherThread);

            act.Should().Throw<ArgumentNullException>();
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
            var subscription = this._eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, eventPriority, ThreadTarget.MainThread);

            subscription.Should().NotBeNull();
            subscription.Priority.Should().Be(eventPriority);
        }

    [TestMethod]
    [DataRow(ThreadTarget.MainThread)]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.PublisherThread)]
    public void ThreadTargetWillBeSetInSubscription(ThreadTarget threadTarget)
    {
            var subscription = this._eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, EventPriority.Normal, threadTarget);

            subscription.Should().NotBeNull();
            subscription.ThreadTarget.Should().Be(threadTarget);
        }

    [TestMethod]
    public void PublishingNullThrowsArgumentNullException()
    {
            Action act = () => this._eventAggregator.Publish<TestEvent>(null);

            act.Should().Throw<ArgumentNullException>();
        }

    [TestMethod]
    public void PublishingEventPassesCorrectInstance()
    {
            TestEvent receivedEvent = null;
            var sentEvent = new TestEvent();

            this._eventAggregator.Subscribe<TestEvent>(e => receivedEvent = e, false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(sentEvent);

            receivedEvent.Should().BeSameAs(sentEvent);
        }

    [TestMethod]
    public void SubscribeMultipleTimesToEventInvokesMultipleHandlers()
    {
            var calls = new List<int>();

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());

            calls.Should()
                 .HaveCount(3)
                 .And.Contain(new [] {1, 2, 3});
        }

    [TestMethod]
    public void EventPriorityWillBeInvokedInCorrectOrder()
    {
            var calls = new List<int>();

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), false, EventPriority.Lowest, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), false, EventPriority.Low, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(4), false, EventPriority.High, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(5), false, EventPriority.Highest, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(6), false, EventPriority.Monitor, ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());

            calls.Should()
                 .HaveCount(6)
                 .And.ContainInOrder(1, 2, 3, 4, 5, 6);
        }

    [TestMethod]
    public void ThrowingExceptionInsideHandlerExecutesAllOtherHandlers()
    {
            var calls = new List<int>();

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), false, EventPriority.Lowest, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), false, EventPriority.Low, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => throw new Exception("OMEGALUL"), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), false, EventPriority.High, ThreadTarget.PublisherThread);

            Action act = () => this._eventAggregator.Publish(new TestEvent());

            act.Should().NotThrow();

            calls.Should()
                 .HaveCount(3)
                 .And.ContainInOrder(1, 2, 3);
        }

    [TestMethod]
    public void PublishingOtherEventDoesNothing()
    {
            Action act = () => this._eventAggregator.Publish(new OtherTestEvent());

            act.Should().NotThrow();
        }

    [TestMethod]
    public void PublishingEventOnlyExecutesGivenEventHandlers()
    {
            var wrongCalled = false;
            var rightCalled = false;

            this._eventAggregator.Subscribe<TestEvent>(_ => wrongCalled = true, false, EventPriority.Normal, ThreadTarget.PublisherThread);
            this._eventAggregator.Subscribe<OtherTestEvent>(_ => rightCalled = true, false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new OtherTestEvent());

            wrongCalled.Should().BeFalse();
            rightCalled.Should().BeTrue();
        }

    [TestMethod]
    public void UnsubscribingDisposesSubscription()
    {
            var subscription = Substitute.For<ISubscription>();

            this._eventAggregator.Unsubscribe(subscription);

            subscription.Received(1).Dispose();
        }

    [TestMethod]
    public void PassingNullToUnsubscribeThrowsException()
    {
            Action act = () => this._eventAggregator.Unsubscribe(null);

            act.Should().Throw<ArgumentNullException>();
        }

    [TestMethod]
    public void DisposingCreatedSubscriptionUnsubscribesFromAggregator()
    {
            var calledAmount = 0;

            var subscription = this._eventAggregator.Subscribe<TestEvent>(
                                                                         _ => calledAmount++,
                                                                         false,
                                                                         EventPriority.Normal,
                                                                         ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());

            subscription.Dispose();

            this._eventAggregator.Publish(new TestEvent());

            subscription.IsDisposed.Should().BeTrue();
            calledAmount.Should().Be(1);
        }

    [TestMethod]
    public void PublishingEventWithNoIgnoreCancelledWillBeExecuted()
    {
            var calledAmount = 0;

            this._eventAggregator.Subscribe<CancellableEvent>(
                                                             _ => calledAmount++,
                                                             false,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new CancellableEvent());

            calledAmount.Should().Be(1);
        }

    [TestMethod]
    public void PublishingCancellableEventAndIgnoreWithoutActualCancellingWillStillExecute()
    {
            var calledAmount = 0;

            this._eventAggregator.Subscribe<CancellableEvent>(
                                                             _ => calledAmount++,
                                                             true,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new CancellableEvent());

            calledAmount.Should().Be(1);
        }

    [TestMethod]
    public void PublishingCancelledCancellableEventWillIgnoreHandlersThatMarkedSo()
    {
            var wrongCalledAmount = 0;

            this._eventAggregator.Subscribe<CancellableEvent>(
                                                             e => e.Cancelled = true,
                                                             false,
                                                             EventPriority.Low,
                                                             ThreadTarget.PublisherThread);

            this._eventAggregator.Subscribe<CancellableEvent>(
                                                             e => wrongCalledAmount++,
                                                             true,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            var cancelledEvent = this._eventAggregator.Publish(new CancellableEvent());

            cancelledEvent.Cancelled.Should().Be(true);

            wrongCalledAmount.Should().Be(0);
        }

    [TestMethod]
    public void HigherEventPriorityCancellableEventWillHaveNoEffectOnLowerPriority()
    {
            var correctCalledAmount = 0;

            this._eventAggregator.Subscribe<CancellableEvent>(
                                                             e => e.Cancelled = true,
                                                             false,
                                                             EventPriority.High,
                                                             ThreadTarget.PublisherThread);

            this._eventAggregator.Subscribe<CancellableEvent>(
                                                             e => correctCalledAmount++,
                                                             true,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            var cancelledEvent = this._eventAggregator.Publish(new CancellableEvent());

            cancelledEvent.Cancelled.Should().Be(true);

            correctCalledAmount.Should().Be(1);
        }

    [TestMethod]
    public void RemovingSubscriptionDuringEventHandlerWillSkipEvent()
    {
            var wrongCalledAmount = 0;

            var subscription = this._eventAggregator.Subscribe<TestEvent>(
                                                                         e => wrongCalledAmount++,
                                                                         false,
                                                                         EventPriority.High,
                                                                         ThreadTarget.PublisherThread);

            this._eventAggregator.Subscribe<TestEvent>(
                                                      e => subscription.Dispose(),
                                                      false,
                                                      EventPriority.Normal,
                                                      ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());

            wrongCalledAmount.Should().Be(0);
        }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public void DisposingEventInsideSameHandlerRemovesForFurtherPublishes()
    {
            var wasDisposed = false;
            var calledAmount = 0;
            ISubscription subscription = null;

            subscription = this._eventAggregator.Subscribe<TestEvent>(
                                                                     e =>
                                                                     {
                                                                         calledAmount++;

                                                                         if (subscription != null)
                                                                         {
                                                                             wasDisposed = true;
                                                                             subscription.Dispose();
                                                                         }
                                                                     },
                                                                     false,
                                                                     EventPriority.Normal,
                                                                     ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());
            this._eventAggregator.Publish(new TestEvent());

            calledAmount.Should().Be(1);
            wasDisposed.Should().BeTrue();
        }

    [TestMethod]
    public void SubscribingToEventInsideHandlerWillPublishForNextEvent()
    {
            var calledAmount = 0;
            var innerCalledAmount = 0;

            this._eventAggregator.Subscribe<TestEvent>(
                                                      e =>
                                                      {
                                                          calledAmount++;

                                                          this._eventAggregator.Subscribe<TestEvent>(
                                                           i =>innerCalledAmount++,
                                                           false,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread);
                                                      },
                                                      false,
                                                      EventPriority.Normal,
                                                      ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());
            this._eventAggregator.Publish(new TestEvent());

            calledAmount.Should().Be(2);
            innerCalledAmount.Should().Be(1);
        }

    [TestMethod]
    [DataRow(ThreadTarget.BackgroundThread)]
    [DataRow(ThreadTarget.MainThread)]
    public void SubscribingToDataChangingEventInWrongThreadTargetThrowsException(ThreadTarget threadTarget)
    {
            Action act = () => this._eventAggregator.Subscribe<DataChangingEvent>(
             _ => { },
             false,
             EventPriority.Normal,
             threadTarget);

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(IDataChangingEvent)}*");
        }

    [TestMethod]
    public void PublishEventWithDifferentCompileTimeTypeDispatchesRightSubscription()
    {
            IEvent eventData = new TestEvent();
            var calledAmount = 0;

            this._eventAggregator.Subscribe<TestEvent>(e => calledAmount++, false, EventPriority.Normal,
                                                      ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(eventData);

            calledAmount.Should().Be(1);
        }

    [TestMethod]
    public void SubscribingToAsyncEventWillReturnCorrectly()
    {
            var subscription = this._eventAggregator.Subscribe<TestEvent>(
                                                                         _ => Task.CompletedTask,
                                                                         false,
                                                                         EventPriority.Normal,
                                                                         ThreadTarget.PublisherThread);

            subscription.Priority.Should().Be(EventPriority.Normal);
            subscription.IgnoreCancelled.Should().BeFalse();
            subscription.IsDisposed.Should().BeFalse();
            subscription.ThreadTarget.Should().Be(ThreadTarget.PublisherThread);

            subscription.Should()
                        .NotBeNull()
                        .And.BeAssignableTo<ISubscription>();
        }

    [TestMethod]
    public async Task AsyncSubscriptionHandlerWillBeTriggered()
    {
            var calledAmount = 0;

            this._eventAggregator.Subscribe<TestEvent>(
                                                                         _ =>
                                                                         {
                                                                             calledAmount++;

                                                                             return Task.CompletedTask;
                                                                         },
                                                                         false,
                                                                         EventPriority.Normal,
                                                                         ThreadTarget.PublisherThread);

            this._eventAggregator.Publish(new TestEvent());

            await Task.Delay(TimeSpan.FromSeconds(1));

            calledAmount.Should().Be(1);
        }
}