using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs
{
    public static class ContainerBuilderExtensions
    {
        public static void RegisterHubs(this ContainerBuilder builder, params Assembly[] controllerAssemblies)
        {
            var virtualHubs = controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => typeof (IVirtualHub).IsAssignableFrom(type)));
            var normalHubs = controllerAssemblies.SelectMany(asm => asm.GetExportedTypes().Where(type => typeof(IHub).IsAssignableFrom(type) && !typeof(IVirtualHub).IsAssignableFrom(type)));
            
            builder.RegisterTypes(normalHubs.ToArray());

        }
    }
}
