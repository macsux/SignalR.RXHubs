using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Contract;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRSelfHost
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
