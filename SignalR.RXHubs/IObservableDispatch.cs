using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace SignalR.RXHubs
{
//    public interface IObservableDispatch : IObservableDispatch, IObserver<object>
//    {
//        
//    }

    public interface IObservableDispatch : IObserver<object>, IDisposable
    {
        CompositeDisposable Subscription { get; }
        long NextCounter { get; }
        long BufferLength { get; }
        void Ack(int messageId);
    }
}