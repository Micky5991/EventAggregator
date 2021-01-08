using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Micky5991.EventAggregator;
using Micky5991.EventAggregator.Enums;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Tests.TestClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Micky5991.EventAggregator.Tests
{
    [TestClass]
    public class EventAggregatorFixture
    {

        private ILogger<IEventAggregator> logger;
        private ILogger<ISubscription> subscriptionLogger;

        private TestSynchronizationContext synchronizationContext;

        private EventAggregatorService eventAggregator;

        [TestInitialize]
        public void Setup()
        {
            this.logger = new NullLogger<IEventAggregator>();
            this.subscriptionLogger = new NullLogger<ISubscription>();
            this.synchronizationContext = new TestSynchronizationContext();

            this.eventAggregator = new EventAggregatorService(this.logger, this.subscriptionLogger);
            this.eventAggregator.SetMainThreadSynchronizationContext(this.synchronizationContext);
        }

        [TestCleanup]
        public void Teardown()
        {
            this.logger = null;
            this.subscriptionLogger = null;
            this.synchronizationContext = null;

            this.eventAggregator = null;
        }

        [TestMethod]
        public void BuildEventAggregatorWorks()
        {
            var aggregator = new EventAggregatorService(this.logger, this.subscriptionLogger);
        }

        [TestMethod]
        public void BuildEventAggregatorWithLoggerNullThrowsException()
        {
            Action act = () => new EventAggregatorService(null, this.subscriptionLogger);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void BuildEventAggregatorWithSubscriptionLoggerNullThrowsException()
        {
            Action act = () => new EventAggregatorService(this.logger, null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void SubscribeEventAggregatorWithoutSynchronizationContextThrowsException()
        {
            var aggregator = new EventAggregatorService(this.logger, this.subscriptionLogger);

            Action act = () => aggregator.Subscribe<TestEvent>(
                                                               x => {},
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
            var subscription = new EventAggregatorService(this.logger, this.subscriptionLogger)
                                   .Subscribe<TestEvent>(e => {}, EventPriority.Normal, threadTarget);

            subscription.Should().NotBeNull();
        }

        [TestMethod]
        public void SubscribeEventWithTargetThatUsesMainThreadContextThrowsException()
        {
            Action act = () => new EventAggregatorService(this.logger, this.subscriptionLogger)
                                   .Subscribe<TestEvent>(e => {}, EventPriority.Normal, ThreadTarget.MainThread);

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(SynchronizationContext)}*");
        }

        [TestMethod]
        public void SubscribeEventWithUnknownEventPriorityThrowsException()
        {
            Action act = () => this.eventAggregator
                                   .Subscribe<TestEvent>(e => { }, (EventPriority) int.MaxValue, ThreadTarget.PublisherThread);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void SubscribeEventWithUnknownThreadTargetThrowsException()
        {
            Action act = () => this.eventAggregator
                                   .Subscribe<TestEvent>(e => { }, EventPriority.Normal, (ThreadTarget) int.MaxValue);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void SubscribeWithNullAsHandlerThrowsException()
        {
            Action act = () => this.eventAggregator
                                   .Subscribe<TestEvent>(null, EventPriority.Normal, ThreadTarget.PublisherThread);

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
                                   .Subscribe<TestEvent>(e => { }, eventPriority, ThreadTarget.MainThread);

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
                                   .Subscribe<TestEvent>(e => { }, EventPriority.Normal, threadTarget);

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

            this.eventAggregator.Subscribe<TestEvent>(e => receivedEvent = e, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(sentEvent);

            receivedEvent.Should().BeSameAs(sentEvent);
        }

        [TestMethod]
        public void SubscribeMultipleTimesToEventInvokesMultipleHandlers()
        {
            var calls = new List<int>();

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(1), EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), EventPriority.Normal, ThreadTarget.PublisherThread);

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
                .Subscribe<TestEvent>(e => calls.Add(1), EventPriority.Lowest, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), EventPriority.Low, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(4), EventPriority.High, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(5), EventPriority.Highest, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(6), EventPriority.Monitor, ThreadTarget.PublisherThread);

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
                .Subscribe<TestEvent>(e => calls.Add(1), EventPriority.Lowest, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(2), EventPriority.Low, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => throw new Exception("OMEGALUL"), EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator
                .Subscribe<TestEvent>(e => calls.Add(3), EventPriority.High, ThreadTarget.PublisherThread);

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

            this.eventAggregator.Subscribe<TestEvent>(_ => wrongCalled = true, EventPriority.Normal, ThreadTarget.PublisherThread);
            this.eventAggregator.Subscribe<OtherTestEvent>(_ => rightCalled = true, EventPriority.Normal, ThreadTarget.PublisherThread);

            this.eventAggregator.Publish(new OtherTestEvent());

            wrongCalled.Should().BeFalse();
            rightCalled.Should().BeTrue();
        }
    }
}
