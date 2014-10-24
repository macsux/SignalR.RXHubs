using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contract;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {

            var clientProxy = new ClientProxy("http://localhost:8000", "IServerHubProxy");
            
            Console.WriteLine("Two subscriptions are now pumping out messages");
            var sub1 = clientProxy.Subscribe(msg => Console.WriteLine("Sub1 {0} > {1}", msg.User, msg.Message), Console.WriteLine, () => Console.WriteLine("Sub1 Sequence ended"));
            var sub2 = clientProxy.Subscribe(msg => Console.WriteLine("Sub2 {0} > {1}", msg.User, msg.Message), Console.WriteLine, () => Console.WriteLine("Sub2 Sequence ended"));
            Console.ReadLine();
            clientProxy.Connect();

            clientProxy.Add("userA", "hi");
            Console.WriteLine("Press any key to disconnect Sub 1");
            Console.ReadKey();
            sub1.Dispose();

            Console.WriteLine("Press any key to disconnect Sub 2");
            Console.ReadKey();
            sub2.Dispose();

            Console.WriteLine("Observable disconnected, connection to server should still be open and we notified it that we want to unsubscribe message listener");
            Console.WriteLine("Press any key to shut down the app and close connection to server");
            Console.ReadKey();
            clientProxy.Dispose();
        }


       
    }
}
