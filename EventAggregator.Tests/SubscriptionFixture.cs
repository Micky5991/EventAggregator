using System;
using System.Threading;
using EventAggregator.Tests.TestClasses;
using FluentAssertions;
using Micky5991.EventAggregator;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Enums;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventAggregator.Tests
{
    [TestClass]
    public class SubscriptionFixture
    {
        private IEvent passedEvent = null;

        private int handleCounter = 0;

        private bool subscribeStatus = true;

        private int mainThreadId;

        private NullLogger<ISubscription> logger;

        private Action handleAction;

        private TestSynchronizationContext synchronizationContext;

        private Subscription<TestEvent> publisherThreadSubscription = null;

        private Subscription<TestEvent> mainThreadSubscription = null;

        private Subscription<TestEvent> backgroundThreadSubscription = null;

        [TestInitialize]
        public void Setup()
        {
            this.mainThreadId = Thread.CurrentThread.ManagedThreadId;
            this.synchronizationContext = new TestSynchronizationContext();
            this.handleAction = () => { };
            this.logger = new NullLogger<ISubscription>();

            SynchronizationContext.SetSynchronizationContext(this.synchronizationContext);

            this.publisherThreadSubscription = new Subscription<TestEvent>(
                                                            this.logger,
                                                            e =>
                                                            {
                                                                this.passedEvent = e;
                                                                this.handleCounter++;

                                                                this.handleAction();
                                                            },
                                                            EventPriority.Normal,
                                                            ThreadTarget.PublisherThread,
                                                            this.synchronizationContext,
                                                            () => this.subscribeStatus = false);

            this.mainThreadSubscription = new Subscription<TestEvent>(
                                                                      this.logger,
                                                                      e =>
                                                                      {
                                                                          this.passedEvent = e;
                                                                          this.handleCounter++;

                                                                          this.handleAction();
                                                                      },
                                                                      EventPriority.Normal,
                                                                      ThreadTarget.MainThread,
                                                                      this.synchronizationContext,
                                                                      () => this.subscribeStatus = false);

            this.backgroundThreadSubscription = new Subscription<TestEvent>(
                                                                            this.logger,
                                                                            e =>
                                                                            {
                                                                                this.passedEvent = e;
                                                                                this.handleCounter++;

                                                                                this.handleAction();
                                                                            },
                                                                            EventPriority.Normal,
                                                                            ThreadTarget.BackgroundThread,
                                                                            this.synchronizationContext,
                                                                            () => this.subscribeStatus = false);
        }

        [TestCleanup]
        public void Teardown()
        {
            this.handleCounter = 0;
            this.subscribeStatus = true;
            this.publisherThreadSubscription = null;
            this.passedEvent = null;
            this.synchronizationContext = null;

            SynchronizationContext.SetSynchronizationContext(null);
        }

        [TestMethod]
        public void CreationOfSubscriptionShouldWork()
        {
            var called = false;
            var unsubscribed = false;

            new Subscription<TestEvent>(
                                        this.logger,
                                        e => called = true,
                                        EventPriority.Normal,
                                        ThreadTarget.PublisherThread,
                                        this.synchronizationContext,
                                        () => unsubscribed = true);

            called.Should().BeFalse();
            unsubscribed.Should().BeFalse();
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
            var subscription = new Subscription<TestEvent>(
                                        this.logger,
                                        e => {},
                                        priority,
                                        ThreadTarget.PublisherThread,
                                        this.synchronizationContext,
                                        () => {});

            subscription.Priority.Should().Be(priority);
        }

        [TestMethod]
        public void CreationOfSubscriptionWithNullHandlerThrowsException()
        {
            Action act = () => new Subscription<TestEvent>(
                                                           this.logger,
                                                           null,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this.synchronizationContext,
                                                           () => { });

            act.Should().Throw<ArgumentNullException>().WithMessage("*handler*");
        }

        [TestMethod]
        public void CreationOfSubscriptionWithNullUnsubscriberThrowsException()
        {
            Action act = () => new Subscription<TestEvent>(
                                                           this.logger,
                                                           e => { },
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this.synchronizationContext,
                                                           null);

            act.Should().Throw<ArgumentNullException>().WithMessage("*unsubscribeAction*");
        }

        [TestMethod]
        public void CreationOfSubscriptionWithNullLoggerThrowsThrowsException()
        {
            Action act = () => new Subscription<TestEvent>(
                                                           null,
                                                           e => { },
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this.synchronizationContext,
                                                           () => { });

            act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
        }

        [TestMethod]
        public void InvokingEventWithNullThrowsArgumentNullException()
        {
            Action act = () => this.publisherThreadSubscription.Invoke(null);

            act.Should().Throw<ArgumentNullException>("*eventInstance*");
        }

        [TestMethod]
        public void DisposingSubscriptionCallsUnsubscription()
        {
            this.publisherThreadSubscription.Dispose();

            this.subscribeStatus.Should().BeFalse();
        }

        [TestMethod]
        public void DisposingSubscriptionThrowsExceptionsOnMethods()
        {
            this.publisherThreadSubscription.Dispose();

            Action actDispose = () => this.publisherThreadSubscription.Dispose();
            actDispose.Should().Throw<ObjectDisposedException>();

            Action actInvoke = () => this.publisherThreadSubscription.Invoke(new TestEvent());
            actInvoke.Should().Throw<ObjectDisposedException>();
        }

        [TestMethod]
        public void CreatingSubscriptionWithInvalidThreadTargetThrowsException()
        {
            Action act = () => new Subscription<TestEvent>(
                                                           this.logger,
                                                           e => { },
                                                           EventPriority.Normal,
                                                           (ThreadTarget) int.MaxValue,
                                                           this.synchronizationContext,
                                                           () => { });

            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*threadTarget*");
        }

        [TestMethod]
        public void InvokingSubscriptionCallsHandlerWithCorreltArguments()
        {
            var eventData = new TestEvent();

            this.publisherThreadSubscription.Invoke(eventData);

            this.passedEvent.Should().Be(eventData);
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
                this.publisherThreadSubscription.Invoke(new TestEvent());
            }

            this.handleCounter.Should().Be(amount);
        }

        [TestMethod]
        public void InvokingMainThreadSubscriptionCallsSynchronizationContext()
        {
            this.mainThreadSubscription.Invoke(new TestEvent());

            this.synchronizationContext.InvokeAmount.Should().Be(1);
        }

        [TestMethod]
        public void InvokingPublisherThreadSubscriptionDoesNotCallSubscriptionContext()
        {
            this.publisherThreadSubscription.Invoke(new TestEvent());

            this.synchronizationContext.InvokeAmount.Should().Be(0);
        }

        [TestMethod]
        public void InvokingBackgroundThreadSubscriptionDoesNotCallSubscriptionContext()
        {
            this.backgroundThreadSubscription.Invoke(new TestEvent());

            this.synchronizationContext.InvokeAmount.Should().Be(0);
        }

        [TestMethod]
        public void ThrowingExceptionInsideHandlerCatchesException()
        {
            this.handleAction = () => throw new Exception("Test");

            Action act = () => this.publisherThreadSubscription.Invoke(new TestEvent());
            act.Should().NotThrow();

            Action mainAct = () => this.mainThreadSubscription.Invoke(new TestEvent());
            mainAct.Should().NotThrow();

            Action backgroundAct = () => this.backgroundThreadSubscription.Invoke(new TestEvent());
            backgroundAct.Should().NotThrow();
        }
    }
}
