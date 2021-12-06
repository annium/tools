using System.Collections.Generic;
using System.Linq;

namespace Xws.Views;

internal class ClientView : IClientView
{
    public IReadOnlyCollection<string> Usages { get; }
    public string Namespace { get; }
    public string Name { get; }
    public string Type { get; }
    public IReadOnlyCollection<BroadcasterView> Broadcasters { get; }
    public IReadOnlyCollection<EventHandlerView> EventHandlers { get; }
    public IReadOnlyCollection<RequestHandlerView> RequestHandlers { get; }
    public IReadOnlyCollection<RequestResponseHandlerView> RequestResponseHandlers { get; }
    public IReadOnlyCollection<SubscriptionHandlerView> SubscriptionHandlers { get; }

    public ClientView(
        IReadOnlyCollection<string> usages,
        string ns,
        string name,
        string type,
        IReadOnlyCollection<IHandlerView> handlers
    )
    {
        Usages = usages;
        Namespace = ns;
        Name = name;
        Type = type;
        Broadcasters = handlers.OfType<BroadcasterView>().ToArray();
        EventHandlers = handlers.OfType<EventHandlerView>().ToArray();
        RequestHandlers = handlers.OfType<RequestHandlerView>().ToArray();
        RequestResponseHandlers = handlers.OfType<RequestResponseHandlerView>().ToArray();
        SubscriptionHandlers = handlers.OfType<SubscriptionHandlerView>().ToArray();
    }

    public override string ToString() => Name;
}