using System.IO;
using Xws.Extensions;
using Xws.Models;
using Xws.Views;
using static Xws.Helpers.WriterHelper;

namespace Xws.Components.Implementations;

internal class Writer : IWriter
{
    private readonly ITemplateWriter _templateWriter;

    public Writer(
        ITemplateWriter templateWriter
    )
    {
        _templateWriter = templateWriter;
    }

    public void Write(string output, ApiView api)
    {
        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);

        Write(output, "ServiceContainerExtensions", "Templates.ServiceContainerExtensions", api);

        WriteClientRoot(output, api);
    }

    private void WriteClientRoot(string rootDir, ApiView api)
    {
        var rootNs = api.Namespace.ToNamespace();
        var output = GetOutputPath(rootDir, rootNs, api.Client.Namespace.ToNamespace());
        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);

        Write(output, api.Client.Type, "Templates.ClientRoot", api.Client);
        Write(output, api.TestClient.Type, "Templates.TestClientRoot", api.TestClient);

        foreach (var client in api.Client.Clients)
            WriteAbstractClient(rootDir, rootNs, client);
    }

    private void WriteAbstractClient(string rootDir, Namespace rootNs, IClientView abstraction)
    {
        switch (abstraction)
        {
            case ClientContainerView container:
                WriteClientContainer(rootDir, rootNs, container);
                break;
            case ClientView client:
                WriteClient(rootDir, rootNs, client);
                break;
        }
    }

    private void WriteClientContainer(string rootDir, Namespace rootNs, ClientContainerView container, bool root = false)
    {
        var output = GetOutputPath(rootDir, rootNs, container.Namespace.ToNamespace());
        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);

        Write(output, container.Type, root ? "Templates.ClientRoot" : "Templates.ClientContainer", container);

        foreach (var client in container.Clients)
            WriteAbstractClient(rootDir, rootNs, client);
    }

    private void WriteClient(string rootDir, Namespace rootNs, ClientView client)
    {
        var output = GetOutputPath(rootDir, rootNs, client.Namespace.ToNamespace());
        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);

        Write(output, client.Type, "Templates.Client", client);
    }

    private void Write<T>(string output, string fileName, string template, T data)
        where T : class
    {
        File.WriteAllText(Path.Combine(output, $"{fileName}.cs"), _templateWriter.Write(template, data));
    }
}