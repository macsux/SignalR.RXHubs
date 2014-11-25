using System;
using Castle.DynamicProxy;
using Microsoft.AspNet.SignalR.Client;

namespace SignalR.RXHubs.Client
{
    public class ObservableHubProxy<THub> : IDisposable where THub : class
    {
        public ObservableHubProxy(string url, string hubName, ConnectionLostBehavior behavior)
        {
            Connection = new HubConnection(url);
            var generator = new ProxyGenerator();
            Proxy = generator.CreateInterfaceProxyWithoutTarget<THub>(new ObservableHubProxyInterceptor<THub>(Connection, hubName, behavior));
        }

        public HubConnection Connection { get; private set; }
        public THub Proxy { get; private set; }
        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}
