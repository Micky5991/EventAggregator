using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using FluentAssertions;
using Micky5991.EventAggregator;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Services;
using Micky5991.EventAggregator.Tests.TestClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Micky5991.EventAggregator.Tests
{
    [TestClass]
    public class EventAggregatorFixture
    {

        private ILogger<ISubscription> subscriptionLogger;

        private TestSynchronizationContext synchronizationContext;

        private EventAggregatorService eventAggregator;

        [TestInitialize]
        public void Setup()
        {
            this.subscriptionLogger = new NullLogger<ISubscription>();
            this.synchronizationContext = new TestSynchronizationContext();

            this.eventAggregator = new EventAggregatorService(this.subscriptionLogger);
            this.eventAggregator.SetMainThreadSynchronizationContext(this.synchronizationContext);
        }

        [TestCleanup]
        public void Teardown()
        {
            this.subscriptionLogger = null;
            this.synchronizationContext = null;

            this.eventAggregator = null;
        }

        [TestMethod]
        public void BuildEventAggregatorWorks()
        {
            var aggregator = new EventAggregatorService(this.subscriptionLogger);
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
            var aggregator = new EventAggregatorService(this.subscriptionLogger);

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
            var subscription = new EventAggregatorService(this.subscriptionLogger)
                                   .Subscribe<TestEvent>(e => {}, false, EventPriority.Normal, threadTarget);

            subscription.Should().NotBeNull();
        }

        [TestMethod]
        public void SubscribeEventWithTargetThatUsesMainThreadContextThrowsException()
        {
            Action act = () => new EventAggregatorService(this.subscriptionLogger)
                                   .Subscribe<TestEvent>(e => {}, false, EventPriority.Normal, ThreadTarget.MainThread);

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(SynchronizationContext)}*");
        }

        [TestMethod]
        public void SubscribeEventWithUnknownEventPriorityThrowsException()
        {
            Action act = () => this.eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, (EventPriority) int.MaxValue, ThreadTarget.PublisherThread);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void SubscribeEventWithUnknownThreadTargetThrowsException()
        {
            Action act = () => this.eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, EventPriority.Normal, (ThreadTarget) int.MaxValue);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void SubscribeWithNullAsHandlerThrowsException()
        {
            Action act = () => this.eventAggregator
                                   .Subscribe<TestEvent>(null, false, EventPriority.Normal, ThreadTarget.PublisherThread);

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
            var subscription = this.eventAggregator
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
            var subscription = this.eventAggregator
                                   .Subscribe<TestEvent>(e => { }, false, EventPriority.Normal, threadTarget);

            subscription.Should().NotBeNull();
            subscription.ThreadTarget.Should().Be(threadTarget);
        }

        [TestMethod]
        public void PublishingNullThrowsArgumentNullException()
        {
            Action act = () => this.eventAggregator.Publish<TestEvent>(null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void PublishingEventPassesCorrectInstance()
        {
            TestEvent receivedEvent = null;
            var sentEvent = new TestEvent();

            this.eventAggregator.Subscribe<TestEvent>(e => receivedEvent = e, false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(sentEvent);

            receivedEvent.Should().BeSameAs(sentEvent);
        }

        [TestMethod]
        public void SubscribeMultipleTimesToEventInvokesMultipleHandlers()
        {
            var calls = new List<int>();

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new TestEvent());

            calls.Should()
                 .HaveCount(3)
                 .And.Contain(new [] {1, 2, 3});
        }

        [TestMethod]
        public void EventPriorityWillBeInvokedInCorrectOrder()
        {
            var calls = new List<int>();

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), false, EventPriority.Lowest, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), false, EventPriority.Low, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(4), false, EventPriority.High, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(5), false, EventPriority.Highest, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(6), false, EventPriority.Monitor, ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new TestEvent());

            calls.Should()
                 .HaveCount(6)
                 .And.ContainInOrder(1, 2, 3, 4, 5, 6);
        }

        [TestMethod]
        public void ThrowingExceptionInsideHandlerExecutesAllOtherHandlers()
        {
            var calls = new List<int>();

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), false, EventPriority.Lowest, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), false, EventPriority.Low, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => throw new Exception("OMEGALUL"), false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), false, EventPriority.High, ThreadTarget.PublisherThread);

            Action act = () => this.eventAggregator.Publish(new TestEvent());

            act.Should().NotThrow();

            calls.Should()
                 .HaveCount(3)
                 .And.ContainInOrder(1, 2, 3);
        }

        [TestMethod]
        public void PublishingOtherEventDoesNothing()
        {
            Action act = () => this.eventAggregator.Publish(new OtherTestEvent());

            act.Should().NotThrow();
        }

        [TestMethod]
        public void PublishingEventOnlyExecutesGivenEventHandlers()
        {
            var wrongCalled = false;
            var rightCalled = false;

            this.eventAggregator.Subscribe<TestEvent>(_ => wrongCalled = true, false, EventPriority.Normal, ThreadTarget.PublisherThread);
            this.eventAggregator.Subscribe<OtherTestEvent>(_ => rightCalled = true, false, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new OtherTestEvent());

            wrongCalled.Should().BeFalse();
            rightCalled.Should().BeTrue();
        }

        [TestMethod]
        public void UnsubscribingDisposesSubscription()
        {
            var subscription = new Mock<ISubscription>();
            subscription.Setup(x => x.Dispose());

            this.eventAggregator.Unsubscribe(subscription.Object);

            subscription.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void PassingNullToUnsubscribeThrowsException()
        {
            Action act = () => this.eventAggregator.Unsubscribe(null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void DisposingCreatedSubscriptionUnsubscribesFromAggregator()
        {
            var calledAmount = 0;

            var subscription = this.eventAggregator.Subscribe<TestEvent>(
                                                                         _ => calledAmount++,
                                                                         false,
                                                                         EventPriority.Normal,
                                                                         ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new TestEvent());

            subscription.Dispose();

            this.eventAggregator.Publish(new TestEvent());

            subscription.IsDisposed.Should().BeTrue();
            calledAmount.Should().Be(1);
        }

        [TestMethod]
        public void PublishingEventWithNoIgnoreCancelledWillBeExecuted()
        {
            var calledAmount = 0;

            this.eventAggregator.Subscribe<CancellableEvent>(
                                                             _ => calledAmount++,
                                                             false,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new CancellableEvent());

            calledAmount.Should().Be(1);
        }

        [TestMethod]
        public void PublishingCancellableEventAndIgnoreWithoutActualCancellingWillStillExecute()
        {
            var calledAmount = 0;

            this.eventAggregator.Subscribe<CancellableEvent>(
                                                             _ => calledAmount++,
                                                             true,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new CancellableEvent());

            calledAmount.Should().Be(1);
        }

        [TestMethod]
        public void PublishingCancelledCancellableEventWillIgnoreHandlersThatMarkedSo()
        {
            var wrongCalledAmount = 0;

            this.eventAggregator.Subscribe<CancellableEvent>(
                                                             e => e.Cancelled = true,
                                                             false,
                                                             EventPriority.Low,
                                                             ThreadTarget.PublisherThread);

            this.eventAggregator.Subscribe<CancellableEvent>(
                                                             e => wrongCalledAmount++,
                                                             true,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            var cancelledEvent = this.eventAggregator.Publish(new CancellableEvent());

            cancelledEvent.Cancelled.Should().Be(true);

            wrongCalledAmount.Should().Be(0);
        }

        [TestMethod]
        public void HigherEventPriorityCancellableEventWillHaveNoEffectOnLowerPriority()
        {
            var correctCalledAmount = 0;

            this.eventAggregator.Subscribe<CancellableEvent>(
                                                             e => e.Cancelled = true,
                                                             false,
                                                             EventPriority.High,
                                                             ThreadTarget.PublisherThread);

            this.eventAggregator.Subscribe<CancellableEvent>(
                                                             e => correctCalledAmount++,
                                                             true,
                                                             EventPriority.Normal,
                                                             ThreadTarget.PublisherThread);

            var cancelledEvent = this.eventAggregator.Publish(new CancellableEvent());

            cancelledEvent.Cancelled.Should().Be(true);

            correctCalledAmount.Should().Be(1);
        }

        [TestMethod]
        public void RemovingSubscriptionDuringEventHandlerWillSkipEvent()
        {
            var wrongCalledAmount = 0;

            var subscription = this.eventAggregator.Subscribe<TestEvent>(
                                                                         e => wrongCalledAmount++,
                                                                         false,
                                                                         EventPriority.High,
                                                                         ThreadTarget.PublisherThread);

            this.eventAggregator.Subscribe<TestEvent>(
                                                      e => subscription.Dispose(),
                                                      false,
                                                      EventPriority.Normal,
                                                      ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new TestEvent());

            wrongCalledAmount.Should().Be(0);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void DisposingEventInsideSameHandlerRemovesForFurtherPublishes()
        {
            var wasDisposed = false;
            var calledAmount = 0;
            ISubscription subscription = null;

            subscription = this.eventAggregator.Subscribe<TestEvent>(
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

            this.eventAggregator.Publish(new TestEvent());
            this.eventAggregator.Publish(new TestEvent());

            calledAmount.Should().Be(1);
            wasDisposed.Should().BeTrue();
        }

        [TestMethod]
        public void SubscribingToEventInsideHandlerWillPublishForNextEvent()
        {
            var calledAmount = 0;
            var innerCalledAmount = 0;

            this.eventAggregator.Subscribe<TestEvent>(
                                                      e =>
                                                      {
                                                          calledAmount++;

                                                          this.eventAggregator.Subscribe<TestEvent>(
                                                           i =>innerCalledAmount++,
                                                           false,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread);
                                                      },
                                                      false,
                                                      EventPriority.Normal,
                                                      ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new TestEvent());
            this.eventAggregator.Publish(new TestEvent());

            calledAmount.Should().Be(2);
            innerCalledAmount.Should().Be(1);
        }

        [TestMethod]
        [DataRow(ThreadTarget.BackgroundThread)]
        [DataRow(ThreadTarget.MainThread)]
        public void SubscribingToDataChangingEventInWrongThreadTargetThrowsException(ThreadTarget threadTarget)
        {
            Action act = () => this.eventAggregator.Subscribe<DataChangingEvent>(
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

            this.eventAggregator.Subscribe<TestEvent>(e => calledAmount++, false, EventPriority.Normal,
                                                      ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(eventData);

            calledAmount.Should().Be(1);
        }
    }
}
