using System;
using System.Reactive.Disposables;

namespace SignalR.RXHubs
{
    public interface IObservableDispatch : IObserver<object>, IDisposable
    {
        CompositeDisposable Subscription { get; }
        long NextCounter { get; }
        long BufferLength { get; }
        void Ack(int messageId);
    }
}