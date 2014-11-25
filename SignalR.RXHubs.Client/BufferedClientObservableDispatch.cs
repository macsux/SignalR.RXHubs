using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs.Client
{
    public class ClientDispatch<T> : IClientDispatch
    {
        public Guid TransportObservableId { get; private set; }
        
        private readonly ConcurrentDictionary<long, ObservableNotification> _buffer = new ConcurrentDictionary<long, ObservableNotification>();
        private readonly IObserver<T> _observer;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private long _nextCounter = 1;

        public ClientDispatch(Guid transportObservableId, IObserver<T> observer, IObservable<ObservableNotification> transportStream)
        {
            TransportObservableId = transportObservableId;
            _observer = observer;
            _subscriptions.Add(transportStream.Where(x => x.SubscriptionId == TransportObservableId).Subscribe(ProcessMessage));
        }

        private void ProcessMessage(ObservableNotification transportMessage)
        {
            var expectedMsgNo = Interlocked.Read(ref _nextCounter);
            if (expectedMsgNo == transportMessage.MsgNumber)
            {
                PropagateMessage(transportMessage);
                ProcessBuffer();
            }
            else
            {
                _buffer.TryAdd(transportMessage.MsgNumber, transportMessage);
            }
        }

        private void ProcessBuffer()
        {
            ObservableNotification expectedMsg;
            while (_buffer.TryRemove(_nextCounter, out expectedMsg))
            {
                PropagateMessage(expectedMsg);
            }
        }
        private void PropagateMessage(ObservableNotification transportMessage)
        {
            switch (transportMessage.Component)
            {
                case ObservableComponent.Next:
                    _observer.OnNext(transportMessage.Message.ToObject<T>());
                    break;
                case ObservableComponent.Error:
                    _observer.OnError(new RemoteException(transportMessage.Message.ToObject<Error>()));
                    break;
                case ObservableComponent.Complete:
                    _observer.OnCompleted();
                    break;
            }
            Interlocked.Increment(ref _nextCounter);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
