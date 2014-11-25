using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SignalR.RXHubs.Client;
using Microsoft.Reactive.Testing;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs.Tests
{
    [TestFixture]
    public class ClientDispatchTests : ReactiveTest
    {
        [Test]
        public void MessagesOutOfOrder_OutputInCorrectOrder()
        {
            var observableId = Guid.NewGuid();
            
            var scheduler = new TestScheduler();
            var observer = scheduler.CreateObserver<string>();
            var transportStream = scheduler.CreateColdObservable(
                OnNext(10, new ObservableNotification(observableId, 1, ObservableComponent.Next, JToken.FromObject("A"))),
                OnNext(20, new ObservableNotification(observableId, 3, ObservableComponent.Next, JToken.FromObject("C"))),
                OnNext(30, new ObservableNotification(observableId, 2, ObservableComponent.Next, JToken.FromObject("B"))),
                OnNext(40, new ObservableNotification(observableId, 4, ObservableComponent.Complete, null))
            );

            var clientDispatch = new ClientDispatch<string>(observableId, observer, transportStream);
            scheduler.Start();
            observer.Messages.AssertEqual(
                OnNext(10, "A"),
                OnNext(30, "B"),
                OnNext(30, "C"),
                OnCompleted<string>(40)
            );
        }
    }

    
}
