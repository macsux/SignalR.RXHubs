using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Contract;
using Microsoft.AspNet.SignalR.Client;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            // Create the connection
            var connection = new HubConnection("http://localhost:8000/signalr");

            // Create the hubproxy
            var hubProxy = connection.CreateHubProxy<IServerHub, IClientEvent>("MyHub");
//            connection.AsObservable<object>().Subscribe(s =>
//            {
//                Console.WriteLine(s);
//            });

            // Subscribe on the event IChatEvents.NewMessage.
            // When the event was fired through the server, the static method Client.NewMessage(string msg) will be invoked.
            //hubProxy.SubscribeOn<string,string>(hub => hub.AddMessage, AddMessage);

            connection.Start().Wait();
            var clientHandler = new ClientHandler(hubProxy);
//            hubProxy.SubscribeOnAll(clientHandler);
            
            var sub = clientHandler.Subscribe(msg => Console.WriteLine("{0} > {1}", msg.User, msg.User));

            // Start the connection
            

            // Call the method IChatHub.GetConnectedClients() on the server and get the result.
//            int clientCount = hubProxy.Call(hub => hub.GetConnectedClients());
//            Console.WriteLine("Connected clients: {0}", clientCount);
//
//            // Call the method IChatHub.SendMessage with no result.
//            hubProxy.Call(hub => hub.SendMessage("Hi, i'm the client."));
            Console.ReadKey();
            sub.Dispose();
            connection.Stop();
        }

        public class ClientHandler : IObservable<ClientMessage>
        {
            private readonly ITypedHubProxy<IServerHub, IClientEvent> _server;
            private readonly IObservable<ClientMessage> _clientObservable;
            public ClientHandler(ITypedHubProxy<IServerHub, IClientEvent> server)
            {
                _server = server;
                _clientObservable = Observable.Create<ClientMessage>(o =>
                {
                    _server.Call(hub => hub.AddMsg(typeof(ClientMessage).Name));
                    _server.SubscribeOn<string, string>(hub => hub.AddMessage, (name, message) => o.OnNext(new ClientMessage() { Message = message, User = name }));
                    return () => _server.Call(hub => hub.RemoveMsg("Test"));
                }).Publish().RefCount();
            }

            public IDisposable Subscribe(IObserver<ClientMessage> observer)
            {
               return  _clientObservable.Subscribe(observer);
            }
        }

        public class ClientMessage
        {
            public string User { get; set; }
            public string Message { get; set; }
        }
    }
}
