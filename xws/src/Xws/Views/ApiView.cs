using System.Collections.Generic;

namespace Xws.Views;

internal class ApiView
{
    public string Namespace { get; }
    public IReadOnlyCollection<string> Usages { get; }
    public string Name { get; }
    public ClientRootView Client { get; }
    public ClientRootView TestClient { get; }

    public ApiView(
        string ns,
        IReadOnlyCollection<string> usages,
        string name,
        ClientRootView client,
        ClientRootView testClient
    )

    {
        Namespace = ns;
        Usages = usages;
        Name = name;
        Client = client;
        TestClient = testClient;
    }
}