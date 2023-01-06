using System;
using Annium.Core.DependencyInjection;
using Xws.Components;
using Xws.Components.Implementations;

namespace Xws;

internal class ServicePack : ServicePackBase
{
    public override void Register(IServiceContainer container, IServiceProvider provider)
    {
        // can't understand that, but preloading assemblies speeds up process greatly, comparing to loading via AssemblyLoadContext
        container.AddRuntime(GetType().Assembly);
        container.AddTime().WithRealTime().SetDefault();
        container.AddSerializers()
            .WithJson(isDefault: true);
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
    }

    public override void Setup(IServiceProvider provider)
    {
        provider.UseLogging(route => route.UseConsole());
    }
}