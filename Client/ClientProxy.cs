using System;
using System.Runtime.InteropServices;
using Contract;
using Microsoft.AspNet.SignalR.Client;

namespace Client
{
    public class ClientProxy : IDisposable, IObservable<ClientMessage>
    {
        private readonly string _hubName;
        private readonly IObservable<ClientMessage> _clientObservable;
        private readonly HubConnection _connection;
        private readonly ITypedHubProxy<IServerHub, IClient> _typedProxy;

        public ClientProxy(string serverSignalRUrl, string hubName)
        {
            _hubName = hubName;
            _connection = new HubConnection(serverSignalRUrl);
            _typedProxy = _connection.CreateHubProxy<IServerHub, IClient>(hubName);
            _clientObservable = _connection.HubSubscriptionAsObservable<ClientMessage, IServerHub>(_hubName, hub => hub.MsgSubscribe());
            
        }

        public void Connect()
        {
            _connection.Start().Wait();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
        
        public IDisposable Subscribe(IObserver<ClientMessage> observer)
        {
            // THIS VERSION WILL MAKE ALL SUBSCRIBERS SHARE A SINGLE SUBSCRIPTION TO SERVER
            return _clientObservable.Subscribe(observer);

            // THIS VERSION WILL ALLOW EACH SUBSCRIBER TO GET IT'S OWN UNIQUE SUBSCRIPTION ON THE SERVER
            // THE ONLY REASON TO WANT TO DO THIS IS TO SETUP SUBSCRIPTIONS WITH DIFFERENT PARAMETERS ON SERVER (pass value into MsgSubscribe)
            return _connection.HubSubscriptionAsObservable<ClientMessage, IServerHub>(_hubName, hub => hub.MsgSubscribe()).Subscribe(observer);
        }

        // can send messages to server
        public void Add(string userName, string message)
        {
            _typedProxy.Call(x => x.Send(new ClientMessage(){Message=message,User=userName}));
        }

    }
}