using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using Contract;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Module = Autofac.Module;

namespace SignalRSelfHost
{
    public class HubsModule : Module
    {
        private Assembly[] _controllerAssemblies;

        public HubsModule() : this(new[] { Assembly.GetEntryAssembly()})
        {
            
        }
        public HubsModule(params Assembly[] controllerAssemblies)
        {
            _controllerAssemblies = controllerAssemblies;
        }

        protected override void Load(ContainerBuilder builder)
        {
//            var realHubInterfaceType = ObservableHub<IServerHub>.GeneratePrivateHubTypeForInterface(generator);
            builder.RegisterType<AutofacHubDescriptorProvider>().AsImplementedInterfaces();

            var virtualHubTypes = _controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => typeof(IVirtualHub).IsAssignableFrom(type)));
            var normalHubTypes = _controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => typeof(IHub).IsAssignableFrom(type) && !typeof(IVirtualHub).IsAssignableFrom(type)));
            builder.RegisterTypes(normalHubTypes.ToArray());

            var generator = new ProxyGenerator();

            foreach (var virtualHubType in virtualHubTypes)
            {
                var realHubInterfaceType = GeneratePrivateHubTypeForInterface(generator, virtualHubType);
                var options = new ProxyGenerationOptions() { BaseTypeForInterfaceProxy = typeof(Hub<IClient>) };
                var virtualHubLocal = virtualHubType;
                builder.Register(context =>
                {
                    
                    var implementedHub = context.Resolve(virtualHubLocal);
                    var retval = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options, new ObservableInterceptor<IClient>(implementedHub));
                    return retval;
                }).As<IHub>().As(proxyType).ExternallyOwned();

            }
            

            builder.Register(
                context =>
                {
                    var implementedHub = context.Resolve<MyHub>();
                    var retval = generator.CreateInterfaceProxyWithoutTarget(realHubInterfaceType, options,
                        new ObservableInterceptor<IClient>(implementedHub));
                    return retval;
                }).As<IHub>().As(proxyType).ExternallyOwned();
        }

        public static Type GeneratePrivateHubTypeForInterface(ProxyGenerator generator, Type publicHubInterface)
        {
            TypeBuilder typeBuilder = generator.ProxyBuilder.ModuleScope.DefineType(false, publicHubInterface.Name, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface);
            typeBuilder.AddInterfaceImplementation(typeof(IHub));
            foreach (var method in publicHubInterface.GetMethods())
            {
                var returnType = method.ReturnType;
                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    returnType = typeof(Guid);
                }
                typeBuilder.DefineMethod(method.Name, method.Attributes, returnType,
                    method.GetParameters().Select(x => x.ParameterType).ToArray());
            }
            var unsubscribeMethod = typeof(ObservableHub<>).GetMethod("Unsubscribe");
            typeBuilder.DefineMethod(unsubscribeMethod.Name, MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.Virtual, unsubscribeMethod.ReturnType,
                unsubscribeMethod.GetParameters().Select(x => x.ParameterType).ToArray());

            var realHubInterfaceType = typeBuilder.CreateType();
            return realHubInterfaceType;
        }
    }
}
