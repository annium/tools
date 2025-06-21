using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Annium.Core.Runtime.Types;
using Annium.Mesh.Server;
using Xws.Extensions;
using Xws.Models;

namespace Xws.Components.Implementations;

internal class Parser : IParser
{
    private const string Handlers = "Handlers";
    private const string Broadcast = "Broadcast";
    private const string Event = "Event";
    private const string Request = "Request";
    private const string SubscriptionInit = "SubscriptionInit";

    public ApiModel Parse(Assembly assembly, string name, ITypeManager tm)
    {
        var broadcasters = GetRawModels(tm, typeof(IBroadcaster<>)).Select(ParseBroadcaster).ToArray();
        var eventHandlers = GetRawModels(tm, typeof(IEventHandler<>)).Select(ParseEventHandler).ToArray();
        var requestHandlers = GetRawModels(tm, typeof(IRequestHandler<,>)).Select(ParseRequestHandler).ToArray();
        var requestResponseHandlers = GetRawModels(tm, typeof(IRequestResponseHandler<,,>))
            .Select(ParseRequestResponseHandler)
            .ToArray();
        var subscriptionHandlers = GetRawModels(tm, typeof(ISubscriptionHandler<,>))
            .Select(ParseSubscriptionHandler)
            .ToArray();

        return new ApiModel(
            assembly,
            name,
            broadcasters,
            eventHandlers,
            requestHandlers,
            requestResponseHandlers,
            subscriptionHandlers
        );
    }

    private BroadcasterModel ParseBroadcaster(RawHandlerModel x) =>
        new(x.Ns, x.Args[0].Name.Replace(Broadcast, string.Empty), x.Args[0]);

    private EventHandlerModel ParseEventHandler(RawHandlerModel x) =>
        new(x.Ns, x.Args[0].Name.Replace(Event, string.Empty), x.Args[0]);

    private RequestHandlerModel ParseRequestHandler(RawHandlerModel x) =>
        new(x.Ns, x.Args[0].Name.Replace(Request, string.Empty), x.Args[0]);

    private RequestResponseHandlerModel ParseRequestResponseHandler(RawHandlerModel x) =>
        new(x.Ns, x.Args[0].Name.Replace(Request, string.Empty), x.Args[0], x.Args[1]);

    private SubscriptionHandlerModel ParseSubscriptionHandler(RawHandlerModel x) =>
        new(x.Ns, x.Args[0].Name.Replace(SubscriptionInit, string.Empty), x.Args[0], x.Args[1]);

    private IEnumerable<RawHandlerModel> GetRawModels(ITypeManager tm, Type target)
    {
        var targetName = target.FullName;

        return tm
            .Types.Select(x =>
                (
                    type: x,
                    ifaces: x.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == targetName)
                        .ToArray()
                )
            )
            .Where(x => x.ifaces.Length > 0)
            .SelectMany(x =>
            {
                var nsParts = (x.type.Namespace ?? string.Empty)
                    .ToNamespaceArray()
                    .SkipWhile(x => x != Handlers)
                    .Skip(1)
                    .ToList();
                var ns = Namespace.New(nsParts);

                return x.ifaces.Select(i => new RawHandlerModel(x.type, i.GetGenericArguments(), ns));
            });
    }

    private record RawHandlerModel(Type Type, Type[] Args, Namespace Ns);
}
