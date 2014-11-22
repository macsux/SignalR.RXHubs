using System;
using System.Reactive.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.RXHubs.Sample.Contract;

namespace SignalR.RXHubs.Sample.Server
{
    [HubName("MyHub")]
    public class MyHub : ObservableHub<IClient>, IServerHub
    {

        public void Send(ClientMessage message)
        {
            Console.WriteLine("{2}: {0} > {1}", message.User, message.Message, Context.ConnectionId);
            Clients.All.AddMessage(message);
        }

        public IObservable<ClientMessage> GetClientMessageObservable()
        {
            return Observable.Interval(TimeSpan.FromSeconds(1)).Select(x => new ClientMessage {Message = Guid.NewGuid().ToString(), User = "Server"});
        }

        public IObservable<string> Test()
        {
            return Observable.Interval(TimeSpan.FromSeconds(1)).Select(x => "Test");
        }
        public void RemoveMsg(string msgType)
        {
            Console.WriteLine("{0} removed", msgType);
        }

        public void AddMsg(string msgType)
        {
            Console.WriteLine("{0} added", msgType);
        }
    }
}