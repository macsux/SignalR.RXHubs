using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.AspNet.SignalR.Client;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            
            var clientHandler = new ClientHandler();
            Console.WriteLine("Two subscriptions are now pumping out messages");
            var sub1 = clientHandler.Subscribe(msg => Console.WriteLine("Sub1 {0} > {1}", msg.User, msg.Message), err => Console.WriteLine(err));
            var sub2 = clientHandler.Subscribe(msg => Console.WriteLine("Sub2 {0} > {1}", msg.User, msg.Message), err => Console.WriteLine(err));
           
            Console.WriteLine("Press any key to disconnect Sub 1");
            Console.ReadKey();
            sub1.Dispose();

            Console.WriteLine("Press any key to disconnect Sub 2");
            Console.ReadKey();
            sub2.Dispose();

            Console.WriteLine("Observable disconnected, connection to server should still be open and we notified it that we want to unsubscribe message listener");
            Console.WriteLine("Press any key to shut down the app and close connection to server");
            Console.ReadKey();
            clientHandler.Dispose();
        }

        public class ClientHandler : IObservable<ClientMessage>, IDisposable
        {
//            private readonly ITypedHubProxy<IServerHub, IClientEvent> _server;
            private readonly IObservable<ClientMessage> _clientObservable;

            HubConnection connection = new HubConnection("http://localhost:8000/signalr");

            ITypedHubProxy<IServerHub, IClientEvent> server;
            
            public ClientHandler()
            {
                server = connection.CreateHubProxy<IServerHub, IClientEvent>("MyHub");
                _clientObservable = Observable.Create<ClientMessage>(async o =>
                {
                    
                    if (connection.State != ConnectionState.Connected)
                        await connection.Start();
                    server.Call(hub => hub.AddMsg(typeof(ClientMessage).Name));
                    server.SubscribeOn<string, string>(hub => hub.AddMessage, (name, message) => o.OnNext(new ClientMessage() { Message = message, User = name }));
                    return async () => await server.CallAsync(hub => hub.RemoveMsg((typeof (ClientMessage).Name)))
                            .ContinueWith(removalTask =>
                            {
                                if (removalTask.IsFaulted)
                                    Console.WriteLine(removalTask.Exception);
                            });
                }).Publish().RefCount();
            }

            public IDisposable Subscribe(IObserver<ClientMessage> observer)
            {
               return  _clientObservable.Subscribe(observer);
            }

            public void Dispose()
            {
                
                connection.Stop();
                connection.Dispose();
                
            }
        }

        public class ClientMessage
        {
            public string User { get; set; }
            public string Message { get; set; }
        }
    }
}
