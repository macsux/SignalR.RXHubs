using System;
using System.Reactive.Disposables;

namespace SignalR.RXHubs.Core
{
 
    internal static class DisposableExtensions
    {
        public static T DisposeWith<T>(this T observable, CompositeDisposable disposables) where T : IDisposable
        {
            disposables.Add(observable);
            return observable;
        }
    }

}
