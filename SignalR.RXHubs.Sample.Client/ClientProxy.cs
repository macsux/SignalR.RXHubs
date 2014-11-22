//using System;
//using Castle.DynamicProxy;
//using Microsoft.AspNet.SignalR.Client;
//using SignalR.RXHubs.Client;
//using SignalR.RXHubs.Sample.Contract;
//
//namespace SignalR.RXHubs.Sample.Client
//{
//    public class ClientProxy : IDisposable, IServerHub
//    {
//        private readonly string _hubName;
//        private readonly IObservable<ClientMessage> _clientObservable;
//        private readonly HubConnection _connection;
//        private readonly ITypedHubProxy<IServerHub, IClient> _typedProxy;
//        private ObservableHubProxyHelper<IServerHub> _hubProxyHelper;
//
//        public ClientProxy(string serverSignalRUrl, string hubName)
//        {
//            
//            _hubName = hubName;
//            _connection = new HubConnection(serverSignalRUrl);
////            _typedProxy = _connection.CreateHubProxy<IServerHub, IClient>(hubName);
//            var generator = new ProxyGenerator();
//            var proxy =
//                generator.CreateInterfaceProxyWithoutTarget<IServerHub>(new ObservableHubProxyInterceptor<IServerHub>(_connection, _hubName, ConnectionLostBehavior.Error));
////            _hubProxyHelper = new ObservableHubProxyHelper<IServerHub>(_connection, hubName, ConnectionLostBehavior.Error);
//            _clientObservable = proxy.GetClientMessageObservable();
////            _clientObservable = _hubProxyHelper.HubSubscriptionAsObservable(x => x.GetClientMessageObservable(), ConnectionLostBehavior.Error);
//        }
//
//        public void Connect()
//        {
//            _connection.Start().Wait();
//        }
//
//        public void Dispose()
//        {
//            _connection.Dispose();
//        }
//        
//
//        public void Send(ClientMessage message)
//        {
//            _typedProxy.Call(x => x.Send(message));
//        }
//
//        public IObservable<ClientMessage> GetClientMessageObservable()
//        {
//            // THIS VERSION WILL MAKE ALL SUBSCRIBERS SHARE A SINGLE SUBSCRIPTION TO SERVER
//            return _clientObservable;
//            // THIS VERSION WILL ALLOW EACH SUBSCRIBER TO GET IT'S OWN UNIQUE SUBSCRIPTION ON THE SERVER
//            // THE ONLY REASON TO WANT TO DO THIS IS TO SETUP SUBSCRIPTIONS WITH DIFFERENT PARAMETERS ON SERVER (pass value into MsgSubscribe)
//            //return _connection.HubSubscriptionAsObservable<ClientMessage, IServerHub>(_hubName, hub => hub.GetClientMessageObservable(), ConnectionLostBehavior.Error);
//        }
//    }
//}