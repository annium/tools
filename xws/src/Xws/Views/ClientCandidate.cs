using System.Collections.Generic;
using System.Linq;
using Xws.Models;

namespace Xws.Views
{
    internal class ClientCandidate
    {
        public IReadOnlyCollection<Namespace> Usages { get; }
        public Namespace Namespace { get; }
        public string Name { get; }
        public string Type { get; }
        public IReadOnlyCollection<IHandlerView> Handlers { get; }

        public ClientCandidate(
            IReadOnlyCollection<Namespace> usages,
            Namespace ns,
            string name,
            string type,
            IReadOnlyCollection<IHandlerView> handlers
        )
        {
            Usages = usages;
            Namespace = ns;
            Name = name;
            Type = type;
            Handlers = handlers;
        }

        public override string ToString() => Name;

        public static explicit operator ClientView(ClientCandidate x) => new(
            x.Usages.Select(y => y.ToString()).ToArray(),
            x.Namespace.ToString(),
            x.Name,
            x.Type,
            x.Handlers
        );
    }
}