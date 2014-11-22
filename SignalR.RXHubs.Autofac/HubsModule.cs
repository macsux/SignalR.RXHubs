﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using Autofac;
using Castle.DynamicProxy;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Practices.ServiceLocation;
using SignalR.RXHubs.Core;
using Module = Autofac.Module;

namespace SignalR.RXHubs.Autofac
{
    public class HubsModule : Module
    {
        private readonly Assembly[] _controllerAssemblies;

        public HubsModule()
            : this(new[] { Assembly.GetEntryAssembly() })
        {
            
        }
        public HubsModule(Assembly[] controllerAssemblies)
        {
            _controllerAssemblies = controllerAssemblies;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BufferedRetryDispatch>().As<IObservableDispatch>();
            builder.RegisterType<ContainerHubDescriptorProvider>().AsImplementedInterfaces();

            var virtualHubTypes = _controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => !type.IsAbstract && typeof(IVirtualHub).IsAssignableFrom(type)));
            var normalHubTypes = _controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => typeof(IHub).IsAssignableFrom(type) && !typeof(IVirtualHub).IsAssignableFrom(type)));
            builder.RegisterTypes(normalHubTypes.ToArray());
            
            var hubGenerator = new HubFactoryGenerator();
            foreach (var virtualHubType in virtualHubTypes)
            {
                builder.RegisterType(virtualHubType).AsSelf();
                Type type = virtualHubType;
                var virtualHubFactory = new HubFactory(virtualHubType, () => (IHub) ServiceLocator.Current.GetInstance(type));
                var realHubFactory = hubGenerator.GetRealHubFactory(virtualHubFactory);
                builder.Register(context => (object)realHubFactory.Factory()).As<IHub>().As(realHubFactory.HubType).ExternallyOwned();
            }
//
//            var generator = new ProxyGenerator();
//
//            foreach (var virtualHubType in virtualHubTypes)
//            {
//                var virtualHubLocal = virtualHubType;
//                builder.RegisterType(virtualHubLocal).AsSelf();
//
//                var virtualHubInterface =
//                    virtualHubType.GetInterfaces().Where(i => i != typeof(IVirtualHub)).FirstOrDefault(i => typeof(IVirtualHub).IsAssignableFrom(i));
//                if (virtualHubInterface == null)
//                    throw new Exception("Virtual hub must implement a contract interface which implements IVirtualHub");
//                var realHubInterfaceType = GeneratePrivateHubTypeForInterface(generator, virtualHubInterface);
//
//                var options = new ProxyGenerationOptions() { BaseTypeForInterfaceProxy = typeof(HubBase) };
//                // attach a HubName attribute to the proxy class so it mimics name that would have been assigned to "virtual hub"
//                var hubName = virtualHubType.GetHubName();
//                var hubNameAttributeConstructor = typeof(HubNameAttribute).GetConstructor(new[] { typeof(string) });
//                var attributeBuilder = new CustomAttributeBuilder(hubNameAttributeConstructor, new[] { hubName });
//                options.AdditionalAttributes.Add(attributeBuilder);
//
//                // we need to figure out the type that would be generated by the proxy so we can register it in the container as such
//                Type proxyType = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new EmptyInterceptor()).GetType();
//                
//                builder.Register(context =>
//                {
//                    var implementedHub = context.Resolve(virtualHubLocal) as IHub;
//                    var retval = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new ObservableInterceptor(implementedHub));
//                    return retval;
//                }).As<IHub>().As(proxyType).ExternallyOwned();
//            }
        }
//
//        public static Type GeneratePrivateHubTypeForInterface(ProxyGenerator generator, Type publicHubInterface)
//        {
//            TypeBuilder typeBuilder = generator.ProxyBuilder.ModuleScope.DefineType(false, publicHubInterface.Name, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface);
//            typeBuilder.AddInterfaceImplementation(typeof(IHub));
//            foreach (var method in publicHubInterface.GetMethods())
//            {
//                var returnType = method.ReturnType;
//                var parameters = method.GetParameters().Select(x => x.ParameterType).ToList();
//                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
//                {
//                    // change signature of observable methods to return void and expect first parameter to be guid
//                    returnType = null;
//                    parameters.Insert(0, typeof(Guid));
//                }
//                
//                typeBuilder.DefineMethod(method.Name, method.Attributes, returnType,parameters.ToArray());
//            }
//            var unsubscribeMethod = typeof(ObservableHub).GetMethod("Unsubscribe");
//            typeBuilder.DefineMethod(unsubscribeMethod.Name, MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.Virtual, unsubscribeMethod.ReturnType,
//                unsubscribeMethod.GetParameters().Select(x => x.ParameterType).ToArray());
//
//            var realHubInterfaceType = typeBuilder.CreateType();
//            return realHubInterfaceType;
//        }
//        private class EmptyInterceptor : IInterceptor
//        {
//            public void Intercept(IInvocation invocation)
//            {
//
//            }
//        }
    }
}
