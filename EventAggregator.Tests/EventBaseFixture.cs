using FluentAssertions;
using Micky5991.EventAggregator.Elements;
using Micky5991.EventAggregator.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Micky5991.EventAggregator.Tests;

[TestClass]
public class EventBaseFixture
{
    private class TestNormalBase : EventBase
    {

    }

    private class TestCancelBase : EventBase, ICancellableEvent
    {
        public bool Cancelled { get; set; }
    }

    private class FakeCancelBase : EventBase, TestClasses.ICancellableEvent
    {
    }

    private class TestCancelableBase : CancellableEventBase
    {
    }

    [TestMethod]
    public void NonCancellableEventBaseWillSignalCancelabilityRight()
    {
        var testEvent = new TestNormalBase();

        testEvent.IsCancellable().Should().BeFalse();
    }

    [TestMethod]
    public void NonCancellableEventWithInterfaceBaseWillSignalCancelabilityRight()
    {
        var testEvent = new TestCancelBase();

        testEvent.IsCancellable().Should().BeTrue();
    }

    [TestMethod]
    public void CancellableEventBaseWillSignalCancelabilityRight()
    {
        var testEvent = new TestCancelableBase();

        testEvent.IsCancellable().Should().BeTrue();
    }

    [TestMethod]
    public void FakeCancellableEventBaseWillBeCaught()
    {
        var testEvent = new FakeCancelBase();

        testEvent.IsCancellable().Should().BeFalse();
    }
}