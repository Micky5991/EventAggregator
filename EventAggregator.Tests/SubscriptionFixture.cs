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
            this._synchronizationContext = new TestSynchronizationContext();
            this._handleAction = () => { };
            this._logger = new NullLogger<ISubscription>();

            SynchronizationContext.SetSynchronizationContext(this._synchronizationContext);

            this._publisherThreadSubscription = new Subscription<TestEvent>(
                                                            this._logger,
                                                            e =>
                                                            {
                                                                this._passedEvent = e;
                                                                this._handleCounter++;

                                                                this._handleAction();
                                                            },
                                                            false,
                                                            EventPriority.Normal,
                                                            ThreadTarget.PublisherThread,
                                                            this._synchronizationContext,
                                                            () => this._subscribeStatus = false);

            this._mainThreadSubscription = new Subscription<TestEvent>(
                                                                      this._logger,
                                                                      e =>
                                                                      {
                                                                          this._passedEvent = e;
                                                                          this._handleCounter++;

                                                                          this._handleAction();
                                                                      },
                                                                      false,
                                                                      EventPriority.Normal,
                                                                      ThreadTarget.MainThread,
                                                                      this._synchronizationContext,
                                                                      () => this._subscribeStatus = false);

            this._backgroundThreadSubscription = new Subscription<TestEvent>(
                                                                            this._logger,
                                                                            e =>
                                                                            {
                                                                                this._passedEvent = e;
                                                                                this._handleCounter++;

                                                                                this._handleAction();
                                                                            },
                                                                            false,
                                                                            EventPriority.Normal,
                                                                            ThreadTarget.BackgroundThread,
                                                                            this._synchronizationContext,
                                                                            () => this._subscribeStatus = false);
        }

    [TestCleanup]
    public void Teardown()
    {
            this._handleCounter = 0;
            this._subscribeStatus = true;
            this._publisherThreadSubscription = null;
            this._passedEvent = null;
            this._synchronizationContext = null;

            SynchronizationContext.SetSynchronizationContext(null);
        }

    [TestMethod]
    public void CreationOfSubscriptionShouldWork()
    {
            var called = false;
            var unsubscribed = false;

            var subscription = new Subscription<TestEvent>(
                                        this._logger,
                                        e => called = true,
                                        false,
                                        EventPriority.Normal,
                                        ThreadTarget.PublisherThread,
                                        this._synchronizationContext,
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
                                                           this._logger,
                                                           e => {},
                                                           false,
                                                           priority,
                                                           ThreadTarget.PublisherThread,
                                                           this._synchronizationContext,
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
                                                           this._logger,
                                                           e => {},
                                                           false,
                                                           EventPriority.Normal,
                                                           threadTarget,
                                                           this._synchronizationContext,
                                                           () => {});

            subscription.ThreadTarget.Should().Be(threadTarget);
        }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SubscriptionIgnoreCancelledWillBeSet(bool ignoreCancelled)
    {
            var subscription = new Subscription<TestEvent>(
                                                           this._logger,
                                                           e => {},
                                                           ignoreCancelled,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this._synchronizationContext,
                                                           () => {});

            subscription.IgnoreCancelled.Should().Be(ignoreCancelled);
        }

    [TestMethod]
    public void CreationOfSubscriptionWithNullHandlerThrowsException()
    {
            Action act = () => new Subscription<TestEvent>(
                                                           this._logger,
                                                           null,
                                                           false,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this._synchronizationContext,
                                                           () => { });

            act.Should().Throw<ArgumentNullException>().WithMessage("*handler*");
        }

    [TestMethod]
    public void CreationOfSubscriptionWithNullUnsubscriberThrowsException()
    {
            Action act = () => new Subscription<TestEvent>(
                                                           this._logger,
                                                           e => { },
                                                           false,
                                                           EventPriority.Normal,
                                                           ThreadTarget.PublisherThread,
                                                           this._synchronizationContext,
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
                                                           this._synchronizationContext,
                                                           () => { });

            act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
        }

    [TestMethod]
    public void InvokingEventWithNullThrowsArgumentNullException()
    {
            Action act = () => this._publisherThreadSubscription.Invoke(null);

            act.Should().Throw<ArgumentNullException>("*eventInstance*");
        }

    [TestMethod]
    public void DisposingSubscriptionCallsUnsubscription()
    {
            this._publisherThreadSubscription.Dispose();

            this._subscribeStatus.Should().BeFalse();
            this._publisherThreadSubscription.IsDisposed.Should().BeTrue();
        }

    [TestMethod]
    public void DisposingSubscriptionThrowsExceptionsOnMethods()
    {
            this._publisherThreadSubscription.Dispose();

            Action actDispose = () => this._publisherThreadSubscription.Dispose();
            actDispose.Should().Throw<ObjectDisposedException>();

            Action actInvoke = () => this._publisherThreadSubscription.Invoke(new TestEvent());
            actInvoke.Should().Throw<ObjectDisposedException>();
        }

    [TestMethod]
    public void CreatingSubscriptionWithInvalidThreadTargetThrowsException()
    {
            Action act = () => new Subscription<TestEvent>(
                                                           this._logger,
                                                           e => { },
                                                           false,
                                                           EventPriority.Normal,
                                                           (ThreadTarget) int.MaxValue,
                                                           this._synchronizationContext,
                                                           () => { });

            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*threadTarget*");
        }

    [TestMethod]
    public void InvokingSubscriptionCallsHandlerWithCorreltArguments()
    {
            var eventData = new TestEvent();

            this._publisherThreadSubscription.Invoke(eventData);

            this._passedEvent.Should().Be(eventData);
        }

    [TestMethod]
    public void InvokingEventWithWrongInstanceThrowsArgumentException()
    {
            var eventData = new OtherTestEvent();

            Action act = () => this._publisherThreadSubscription.Invoke(eventData);

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
                this._publisherThreadSubscription.Invoke(new TestEvent());
            }

            this._handleCounter.Should().Be(amount);
        }

    [TestMethod]
    public void InvokingMainThreadSubscriptionCallsSynchronizationContext()
    {
            this._mainThreadSubscription.Invoke(new TestEvent());

            this._synchronizationContext.InvokeAmount.Should().Be(1);
        }

    [TestMethod]
    public void InvokingPublisherThreadSubscriptionDoesNotCallSubscriptionContext()
    {
            this._publisherThreadSubscription.Invoke(new TestEvent());

            this._synchronizationContext.InvokeAmount.Should().Be(0);
        }

    [TestMethod]
    public void InvokingBackgroundThreadSubscriptionDoesNotCallSubscriptionContext()
    {
            this._backgroundThreadSubscription.Invoke(new TestEvent());

            this._synchronizationContext.InvokeAmount.Should().Be(0);
        }

    [TestMethod]
    public void ThrowingExceptionInsideHandlerCatchesException()
    {
            this._handleAction = () => throw new Exception("Test");

            Action act = () => this._publisherThreadSubscription.Invoke(new TestEvent());
            act.Should().NotThrow();

            Action mainAct = () => this._mainThreadSubscription.Invoke(new TestEvent());
            mainAct.Should().NotThrow();

            Action backgroundAct = () => this._backgroundThreadSubscription.Invoke(new TestEvent());
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
                                                                   this._logger,
                                                                   e => { e.Number = 2; },
                                                                   false,
                                                                   EventPriority.Normal,
                                                                   ThreadTarget.PublisherThread,
                                                                   this._synchronizationContext,
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
                                                                   this._logger,
                                                                   _ => { },
                                                                   false,
                                                                   EventPriority.Normal,
                                                                   threadTarget,
                                                                   this._synchronizationContext,
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