using System.Collections.Generic;
using System.Reflection;

namespace Xws.Models;

public class ApiModel
{
    public Assembly Assembly { get; }
    public string Project { get; }
    public IReadOnlyCollection<BroadcasterModel> Broadcasters { get; }
    public IReadOnlyCollection<EventHandlerModel> EventHandlers { get; }
    public IReadOnlyCollection<RequestHandlerModel> RequestHandlers { get; }
    public IReadOnlyCollection<RequestResponseHandlerModel> RequestResponseHandlers { get; }
    public IReadOnlyCollection<SubscriptionHandlerModel> SubscriptionHandlers { get; }

    public ApiModel(
        Assembly assembly,
        string project,
        IReadOnlyCollection<BroadcasterModel> broadcasters,
        IReadOnlyCollection<EventHandlerModel> eventHandlers,
        IReadOnlyCollection<RequestHandlerModel> requestHandlers,
        IReadOnlyCollection<RequestResponseHandlerModel> requestResponseHandlers,
        IReadOnlyCollection<SubscriptionHandlerModel> subscriptionHandlers
    )
    {
        Assembly = assembly;
        Project = project;
        Broadcasters = broadcasters;
        EventHandlers = eventHandlers;
        RequestHandlers = requestHandlers;
        RequestResponseHandlers = requestResponseHandlers;
        SubscriptionHandlers = subscriptionHandlers;
    }
}