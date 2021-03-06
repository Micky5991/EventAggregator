using System;
using System.Threading;
using FluentAssertions;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Tests.TestClasses;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ICancellableEvent = Micky5991.EventAggregator.Interfaces.ICancellableEvent;

namespace Micky5991.EventAggregator.Tests
{
    [TestClass]
    public class SubscriptionFixture
    {
        private IEvent passedEvent;

        private int handleCounter;

        private bool subscribeStatus = true;

        private NullLogger<ISubscription> logger;

        private Action handleAction;

        private TestSynchronizationContext synchronizationContext;

        private Subscription<TestEvent> publisherThreadSubscription;

        private Subscription<TestEvent> mainThreadSubscription;

        private Subscription<TestEvent> backgroundThreadSubscription;

        [TestInitialize]
        public void Setup()
        {
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
                                                            false,
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
                                                                      false,
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
                                                                            false,
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

            var subscription = new Subscription<TestEvent>(
                                        this.logger,
                                        e => called = true,
                                        false,
                                        EventPriority.Normal,
                                        ThreadTarget.PublisherThread,
                                        this.synchronizationContext,
                                        () => unsubscribed = true);

            called.Should().BeFalse();
            unsubscribed.Should().BeFalse();
            subscription.IsDisposed.Should().BeFalse();
            subscription.Type.Should().Be(typeof(TestEvent));
            subscription.IgnoreCancelled.Should().BeFalse();
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
                                                           false,
                                                           priority,
                                                           ThreadTarget.PublisherThread,
                                                           this.synchronizationContext,
                                                           () => {});

            subscription.Priority.Should().Be(priority);
        }

        [TestMethod]
        [DataRow(ThreadTarget.BackgroundThread)]
        [DataRow(ThreadTarget.MainThread)]
        [DataRow(ThreadTarget.PublisherThread)]
        public void SubscriptionThreadTargetWillBeSet(ThreadTarget threadTarget)
        {
            var subscription = new Subscription<TestEvent>(
                                                           this.logger,
                                                           e => {},
                                                           false,
                                                           EventPriority.Normal,
                                                           threadTarget,
                                                           this.synchronizationContext,
                                                           () => {});

            subscription.ThreadTarget.Should().Be(threadTarget);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SubscriptionIgnoreCancelledWillBeSet(bool ignoreCancelled)
        {
            var subscription = new Subscription<TestEvent>(
                                                           this.logger,
                                                           e => {},
                                                           ignoreCancelled,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this.synchronizationContext,
                                                           () => {});

            subscription.IgnoreCancelled.Should().Be(ignoreCancelled);
        }

        [TestMethod]
        public void CreationOfSubscriptionWithNullHandlerThrowsException()
        {
            Action act = () => new Subscription<TestEvent>(
                                                           this.logger,
                                                           null,
                                                           false,
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
                                                           false,
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
                                                           false,
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
            this.publisherThreadSubscription.IsDisposed.Should().BeTrue();
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
                                                           false,
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
        public void InvokingEventWithWrongInstanceThrowsArgumentException()
        {
            var eventData = new OtherTestEvent();

            Action act = () => this.publisherThreadSubscription.Invoke(eventData);

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

        [TestMethod]
        public void InvokingDataChangingEventWillKeepInstanceSame()
        {
            var eventData = new DataChangingEvent
            {
                Number = 5,
            };

            var subscription = new Subscription<DataChangingEvent>(
                                                                   this.logger,
                                                                   e => { e.Number = 2; },
                                                                   false,
                                                                   EventPriority.Normal,
                                                                   ThreadTarget.PublisherThread,
                                                                   this.synchronizationContext,
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
            Action act = () => new Subscription<DataChangingEvent>(
                                                                   this.logger,
                                                                   _ => { },
                                                                   false,
                                                                   EventPriority.Normal,
                                                                   threadTarget,
                                                                   this.synchronizationContext,
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
}
