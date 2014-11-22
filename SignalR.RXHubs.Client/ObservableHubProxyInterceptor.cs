using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.AspNet.SignalR.Client;

namespace SignalR.RXHubs.Client
{
    internal class ObservableHubProxyInterceptor<T> : IInterceptor where T : class
    {
        private readonly ObservableHubProxyHelper<T> _proxyHelper;
        public ObservableHubProxyInterceptor(HubConnection connection, string hubName,  ConnectionLostBehavior behavior)
        {
            _proxyHelper = new ObservableHubProxyHelper<T>(connection, hubName, behavior);
        }
        public void Intercept(IInvocation invocation)
        {
            var targetMethod = invocation.Method;
            if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof (IObservable<>))
            {
                var observableTypeArg = targetMethod.ReturnType.GetGenericArguments().First();

                var hubSubscriptionMethod = _proxyHelper.GetType().GetMethod("HubSubscriptionAsObservable",new []{typeof(string),typeof(IEnumerable<object>)}).MakeGenericMethod(observableTypeArg);
                invocation.ReturnValue = hubSubscriptionMethod.Invoke(_proxyHelper, new object[]{targetMethod.Name, invocation.Arguments});
            }
            else
            {
                var result = _proxyHelper.HubProxy.Invoke(invocation.Method.Name, invocation.Arguments);
                if (targetMethod.ReturnType.IsAssignableFrom(typeof (Task)))
                    invocation.ReturnValue = result;
            }
        }
    }
}
