using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Castle.DynamicProxy;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs
{
    /// <summary>
    /// Builds dynamic proxies that wrap virtual hubs into real hubs. Output is to be registered with IoC as IHub
    /// </summary>
    public class HubFactoryGenerator : IHubFactoryGenerator
    {
        private static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();

        public HubFactory GetRealHubFactory(HubFactory virtualHubFactory)
        {
            var virtualHubInterface =
                virtualHubFactory.HubType.GetInterfaces().Where(i => i != typeof(IVirtualHub)).FirstOrDefault(i => typeof(IVirtualHub).IsAssignableFrom(i));
            if (virtualHubInterface == null)
                throw new Exception("Virtual hub must implement a contract interface which implements IVirtualHub");
            var realHubInterfaceType = GeneratePrivateHubTypeForInterface(ProxyGenerator, virtualHubInterface);

            var options = new ProxyGenerationOptions() { BaseTypeForInterfaceProxy = typeof(HubBase) };
            // attach a HubName attribute to the proxy class so it mimics name that would have been assigned to "virtual hub"
            var hubName = virtualHubFactory.HubType.GetHubName();
            var hubNameAttributeConstructor = typeof(HubNameAttribute).GetConstructor(new[] { typeof(string) });
            // ReSharper disable once AssignNullToNotNullAttribute
            var attributeBuilder = new CustomAttributeBuilder(hubNameAttributeConstructor, new object[] { hubName });
            options.AdditionalAttributes.Add(attributeBuilder);

            // we need to figure out the type that would be generated by the proxy so we can register it in the container as such
            Type proxyType = ProxyGenerator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new EmptyInterceptor()).GetType();

            var factory = new HubFactory(proxyType, () =>
            {
                var implementedHub = virtualHubFactory.Factory();
                var retval = (IHub)ProxyGenerator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new ObservableInterceptor(implementedHub));
                return retval;
            });
            return factory;
//
//            builder.Register(context =>
//            {
//                var implementedHub = context.Resolve(virtualHubLocal) as IHub;
//                var retval = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new ObservableInterceptor(implementedHub));
//                return retval;
//            }).As<IHub>().As(proxyType).ExternallyOwned();
        }
        private static Type GeneratePrivateHubTypeForInterface(ProxyGenerator generator, Type publicHubInterface)
        {
            TypeBuilder typeBuilder = generator.ProxyBuilder.ModuleScope.DefineType(false, publicHubInterface.Name, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface);
            typeBuilder.AddInterfaceImplementation(typeof(IHub));
            foreach (var method in publicHubInterface.GetMethods())
            {
                var returnType = method.ReturnType;
                var parameters = method.GetParameters().Select(x => x.ParameterType).ToList();
                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    // change signature of observable methods to return void and expect first parameter to be guid
                    returnType = null;
                    parameters.Insert(0, typeof(Guid));
                }

                typeBuilder.DefineMethod(method.Name, method.Attributes, returnType, parameters.ToArray());
            }
            var unsubscribeMethod = typeof(ObservableHub<>).GetMethod("Unsubscribe");
            typeBuilder.DefineMethod(unsubscribeMethod.Name, MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.Virtual, unsubscribeMethod.ReturnType,
                unsubscribeMethod.GetParameters().Select(x => x.ParameterType).ToArray());
            var ackMethod = typeof(ObservableHub<>).GetMethod("Ack");
            typeBuilder.DefineMethod(ackMethod.Name, MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.Virtual, ackMethod.ReturnType,
                ackMethod.GetParameters().Select(x => x.ParameterType).ToArray());

            var realHubInterfaceType = typeBuilder.CreateType();
            return realHubInterfaceType;
        }
        private class EmptyInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {

            }
        }
    }
}