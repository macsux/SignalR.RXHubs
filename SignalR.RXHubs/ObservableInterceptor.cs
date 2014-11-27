using System;
using System.Linq;
using Castle.DynamicProxy;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.RXHubs
{
    public class ObservableInterceptor : IInterceptor
    {
        private readonly IHub _implementation;

        public ObservableInterceptor(IHub virtualHub)
        {
            _implementation = virtualHub;
        }

        public void Intercept(IInvocation invocation)
        {
            var targetMethod = _implementation.GetType()
                .GetMethod(invocation.Method.Name);

            // clients is a special property because it's declare different (return type) ony IHub and Hub<>. We need to make sure that invokation is forwarded to IHub
            if (invocation.Method.Name == "get_Clients")
            {
                invocation.ReturnValue = typeof(IHub).GetProperty("Clients").GetValue(_implementation);
            }
            else if (invocation.Method.Name == "set_Clients")
            {
                typeof(IHub).GetProperty("Clients").SetValue(_implementation,invocation.Arguments.First());
            }
            else if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                var subscriptionId = (Guid) invocation.Arguments[0];
                var observable = targetMethod.Invoke(_implementation, invocation.Arguments.Skip(1).ToArray());
                var subscribeMethod = _implementation.GetType().GetMethod("SubscribeCallerToObservable").MakeGenericMethod(observable.GetType().GetGenericArguments().Last());
                invocation.ReturnValue = subscribeMethod.Invoke(_implementation, new[] {subscriptionId,observable});
            }
            else
            {
                invocation.ReturnValue = _implementation.GetType().GetMethod(invocation.Method.Name).Invoke(_implementation, invocation.Arguments);    
            }
        }
    }
}