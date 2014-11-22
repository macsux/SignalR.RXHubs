using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json;

namespace SignalR.RXHubs.Client
{
    public static class HubConnectionExtensions
    {
       
        public static IHubProxy GetHubProxy(this HubConnection hubConnection, string hubName)
        {
            FieldInfo field = hubConnection.GetType().GetField("_hubs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new ConstraintException("Couldn't find \"_hubs\" field inside of the HubConnection.");
            var dictionary = (Dictionary<string, HubProxy>)field.GetValue(hubConnection);
            if (dictionary.ContainsKey(hubName))
                return dictionary[hubName];
            else
                return null;
        }
        internal static ActionDetail GetActionDetails<TInput, TResult>(this Expression<Func<TInput, TResult>> action)
        {

            var callExpression = (MethodCallExpression)action.Body;

            var actionDetail = new ActionDetail
            {
                MethodName = callExpression.Method.Name,
                Parameters = callExpression.Arguments.Select(ConvertToConstant).ToArray(),
                ReturnType = typeof(TResult)
            };

            return actionDetail;
        }
        private static object ConvertToConstant(Expression expression)
        {
            UnaryExpression objectMember = Expression.Convert(expression, typeof(object));
            Expression<Func<object>> getterLambda = Expression.Lambda<Func<object>>(objectMember);
            Func<object> getter = getterLambda.Compile();

            return getter();
        }
        internal class ActionDetail
        {
            public string MethodName { get; set; }
            public object[] Parameters { get; set; }
            public Type ReturnType { get; set; }
        }
    }
}