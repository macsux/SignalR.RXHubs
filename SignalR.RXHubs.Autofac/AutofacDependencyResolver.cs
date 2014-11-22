// Type: Autofac.Integration.SignalR.AutofacDependencyResolver
// Assembly: Autofac.Integration.SignalR, Version=3.1.0.0, Culture=neutral, PublicKeyToken=17863af14b0044da
// MVID: 6596347A-92B2-4F91-B699-EA9FDB8EE3AD
// Assembly location: C:\projects\SignalRSelfHost\packages\Autofac.SignalR2.3.1.0\lib\net45\Autofac.Integration.SignalR.dll

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.AspNet.SignalR;

namespace SignalR.RXHubs.Autofac
{
    /// <summary>
    /// Autofac implementation of the <see cref="T:Microsoft.AspNet.SignalR.IDependencyResolver"/> interface.
    /// 
    /// </summary>
    public class AutofacDependencyResolver : DefaultDependencyResolver
    {
        private readonly ILifetimeScope _lifetimeScope;

        /// <summary>
        /// Gets the Autofac implementation of the dependency resolver.
        /// 
        /// </summary>
        public static AutofacDependencyResolver Current
        {
            get
            {
                return GlobalHost.DependencyResolver as AutofacDependencyResolver;
            }
        }

        /// <summary>
        /// Gets the <see cref="T:Autofac.ILifetimeScope"/> that was provided to the constructor.
        /// 
        /// </summary>
        public ILifetimeScope LifetimeScope
        {
            get
            {
                return this._lifetimeScope;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SignalR.RXHubs.Autofac.AutofacDependencyResolver"/> class.
        /// 
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope that services will be resolved from.</param><exception cref="T:System.ArgumentNullException">Thrown if <paramref name="lifetimeScope"/> is <see langword="null"/>.
        ///             </exception>
        public AutofacDependencyResolver(ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException("lifetimeScope");
            this._lifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// Get a single instance of a service.
        /// 
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>
        /// The single instance if resolved; otherwise, <c>null</c>.
        /// </returns>
        public override object GetService(Type serviceType)
        {

            var retval = ResolutionExtensions.ResolveOptional((IComponentContext) this._lifetimeScope,
                (Type) serviceType);
            if(retval == null)
                retval = base.GetService(serviceType);
            return retval;
        }

        /// <summary>
        /// Gets all available instances of a services.
        /// 
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>
        /// The list of instances if any were resolved; otherwise, an empty list.
        /// </returns>
        public override IEnumerable<object> GetServices(Type serviceType)
        {
            IEnumerable<object> source = (IEnumerable<object>)ResolutionExtensions.Resolve((IComponentContext)this._lifetimeScope, (Type)typeof(IEnumerable<>).MakeGenericType(new Type[1]
      {
        serviceType
      }));
            IEnumerable<object> retval;
            if (!Enumerable.Any<object>(source))
                retval = base.GetServices(serviceType);
            else
                retval = source;
            return retval;
        }
    }
}
