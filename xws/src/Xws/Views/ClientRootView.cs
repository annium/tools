using System.Collections.Generic;

namespace Xws.Views
{
    internal class ClientRootView
    {
        public string Namespace { get; }
        public IReadOnlyCollection<string> Usages { get; }
        public string Type { get; }
        public IReadOnlyCollection<IClientView> Clients { get; }

        public ClientRootView(
            string ns,
            IReadOnlyCollection<string> usages,
            string type,
            IReadOnlyCollection<IClientView> clients
        )
        {
            Usages = usages;
            Namespace = ns;
            Type = type;
            Clients = clients;
        }

        public override string ToString() => Type;
    }
}