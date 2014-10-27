using System;
using System.Collections.Generic;
using Dynamitey;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRSelfHost
{
    internal static class HubTypeExtensions
    {
        internal static string GetHubName(this Type type)
        {
            if (!typeof(IHub).IsAssignableFrom(type))
            {
                return null;
            }

            return GetHubAttributeName(type) ?? type.Name;
        }

        internal static string GetHubAttributeName(this Type type)
        {
            if (!typeof(IHub).IsAssignableFrom(type))
            {
                return null;
            }
            // We can still return null if there is no attribute name
            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type, attr => attr.HubName);
        }

        internal static IEnumerable<Type> GetParents(this Type type)
        {
            if (type.BaseType != null)
            {
                yield return type.BaseType;
                foreach (var parent in GetParents(type.BaseType))
                {
                    yield return parent;
                }
            }
        }
    }
}