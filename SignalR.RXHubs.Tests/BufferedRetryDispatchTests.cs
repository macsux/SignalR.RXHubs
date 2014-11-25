using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using SignalR.RXHubs.Core;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace SignalR.RXHubs.Tests
{
    [TestFixture]
    public class BufferedRetryDispatchTests
    {
        [Test]
        public void WhenNoAck_SendRetry()
        {

            int transportInvocations = 0;
            var testScheduler = new TestScheduler();
            testScheduler.AdvanceTo(DateTime.Now.Ticks);
            
            var sut = new BufferedRetryDispatch(Guid.Empty, _ => transportInvocations++, Disposable.Empty, testScheduler);
            sut.OnNext("test");
            testScheduler.AdvanceBy(TimeSpan.FromSeconds(6).Ticks);

            Assert.AreEqual(2, transportInvocations);
            sut.Dispose();
        }

        [Test]
        public void WhenAck_NoRetry()
        {

            int transportInvocations = 0;
            var testScheduler = new TestScheduler();
            testScheduler.AdvanceTo(DateTime.Now.Ticks);
            var sut = new BufferedRetryDispatch(Guid.Empty, _ => transportInvocations++, Disposable.Empty, testScheduler);

            sut.OnNext("test");
            sut.Ack(1);
            testScheduler.AdvanceBy(TimeSpan.FromSeconds(6).Ticks);

            Assert.AreEqual(1, transportInvocations);
            sut.Dispose();
        }

        [Test]
        public void WhenException_TransportError()
        {

            ObservableNotification notification = null;
            var sut = new BufferedRetryDispatch(Guid.Empty, x => notification = x, Disposable.Empty);

            sut.OnError(new Exception("Test"));

            var error = notification.Message.ToObject<Error>();
            Assert.AreEqual(ObservableComponent.Error, notification.Component);
            Assert.AreEqual("Test", error.Message);
            sut.Dispose();
        }

        [Test]
        public void WhenComplete_TransportComplete()
        {
            ObservableNotification notification = null;
            var sut = new BufferedRetryDispatch(Guid.Empty, x => notification = x, Disposable.Empty);

            sut.OnCompleted();

            Assert.AreEqual(ObservableComponent.Complete, notification.Component);
            sut.Dispose();
        }

        [Test]
        public void WhenMultipleNextThenComplete_TransportSendWithProperSequence()
        {
            var notifications = new List<ObservableNotification>();
            var sut = new BufferedRetryDispatch(Guid.Empty, notifications.Add, Disposable.Empty);

            sut.OnNext("A");
            sut.OnNext("B");
            sut.OnCompleted();

            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual(1, notifications[0].MsgNumber);
            Assert.AreEqual(2, notifications[1].MsgNumber);
            Assert.AreEqual(ObservableComponent.Next, notifications[1].Component);
            Assert.AreEqual(3, notifications[2].MsgNumber);
            Assert.AreEqual(ObservableComponent.Complete, notifications[2].Component);
        }
    }
}
