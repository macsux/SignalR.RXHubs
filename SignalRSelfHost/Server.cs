using System;
using Contract;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;

namespace SignalRSelfHost
{
    class Server
    {
        static void Main(string[] args)
        {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses. 
            // See http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx 
            // for more information.
            string url = "http://*:8000";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
    }
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
    public class MyHub : Hub<IClientEvent>, IServerHub
    {
        public void Send(string name, string message)
        {
            Clients.All.AddMessage(name, message);
        }

        public void RemoveMsg(string msgType)
        {
            Console.WriteLine("{0} removed",msgType);
        }
        public void AddMsg(string msgType)
        {
            Console.WriteLine("{0} added", msgType);
        }
    }
}