using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Core.Runtime;
using Annium.Core.Runtime.Loader;
using Annium.Extensions.Arguments;
using Annium.Logging.Console;
using Annium.Logging.Shared;
using Annium.Serialization.Abstractions;
using Annium.Serialization.Json;
using Xws.Components;
using Xws.Components.Implementations;

namespace Xws;

internal class ServicePack : ServicePackBase
{
    public override Task RegisterAsync(IServiceContainer container, IServiceProvider provider, CancellationToken ct)
    {
        // can't understand that, but preloading assemblies speeds up process greatly, comparing to loading via AssemblyLoadContext
        container.AddRuntime(GetType().Assembly);
        container.AddTime().WithRealTime().SetDefault();
        container.AddSerializers().WithJson(isDefault: true);
        container.AddAssemblyLoader();
        container.AddResourceLoader();
        container.AddArguments();
        container.AddLogging();
        container.AddMapper();

        container.Add<ILoader, Loader>().Singleton();
        container.Add<IParser, Parser>().Singleton();
        container.Add<IProcessor, Processor>().Singleton();
        container.Add<ITemplateWriter, TemplateWriter>().Singleton();
        container.Add<IWriter, Writer>().Singleton();

        return Task.CompletedTask;
    }

    public override Task SetupAsync(IServiceProvider provider, CancellationToken ct)
    {
        provider.UseLogging(route => route.UseConsole());

        return Task.CompletedTask;
    }
}
