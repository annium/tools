using System.IO;
using Annium.Core.Runtime.Loader;
using Annium.Core.Runtime.Types;
using Annium.Logging;
using Xws.Models;

namespace Xws.Components.Implementations;

internal class Loader : ILoader, ILogSubject
{
    public ILogger Logger { get; }
    private readonly IAssemblyLoaderBuilder _assemblyLoaderBuilder;
    private readonly IParser _parser;

    public Loader(
        IAssemblyLoaderBuilder assemblyLoaderBuilder,
        IParser parser,
        ILogger logger
    )
    {
        _assemblyLoaderBuilder = assemblyLoaderBuilder;
        _parser = parser;
        Logger = logger;
    }

    public ApiModel Load(string assemblyPath, string projectName)
    {
        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly file '{assemblyPath}' missing.");

        var loader = _assemblyLoaderBuilder.UseFileSystemLoader(Path.GetDirectoryName(assemblyPath)!).Build();
        var name = Path.GetFileNameWithoutExtension(assemblyPath);

        this.Info($"load assembly {name}");
        var assembly = loader.Load(name);
        this.Info($"get assembly {name} TypeManager");
        var tm = TypeManager.GetInstance(assembly, VoidLogger.Instance);
        this.Info($"parse assembly {name} model");
        var model = _parser.Parse(assembly, projectName, tm);
        this.Info($"parsed assembly {name}");

        return model;
    }
}