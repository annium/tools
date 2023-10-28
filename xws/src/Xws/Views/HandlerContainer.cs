using System.Collections.Generic;
using Xws.Models;

namespace Xws.Views;

internal class HandlerContainer
{
    public IReadOnlyCollection<Namespace> Usages { get; }
    public Namespace Namespace { get; }
    public string Name { get; }
    public IHandlerView View { get; }

    public HandlerContainer(IReadOnlyCollection<Namespace> usages, Namespace ns, string name, IHandlerView view)
    {
        Usages = usages;
        Namespace = ns;
        Name = name;
        View = view;
    }
}
