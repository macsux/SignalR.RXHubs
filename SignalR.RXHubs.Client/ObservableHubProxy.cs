using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.AspNet.SignalR.Client;

namespace SignalR.RXHubs.Client
{
    public class ObservableHubProxy<THub> : IDisposable where THub : class
    {
        public ObservableHubProxy(string url, string hubName, ConnectionLostBehavior behavior)
        {
            this.Connection = new HubConnection(url);
            var generator = new ProxyGenerator();
            this.Proxy = generator.CreateInterfaceProxyWithoutTarget<THub>(new ObservableHubProxyInterceptor<THub>(Connection, hubName, behavior));
        }

        public HubConnection Connection { get; private set; }
        public THub Proxy { get; private set; }
        public void Dispose()
        {
            this.Connection.Dispose();
            
        }
    }
}
