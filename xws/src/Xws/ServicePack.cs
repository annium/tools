using System;
using Annium.Core.DependencyInjection;
using Xws.Components;
using Xws.Components.Implementations;

namespace Xws
{
    internal class ServicePack : ServicePackBase
    {
        public override void Register(IServiceContainer container, IServiceProvider provider)
        {
            container.AddRuntimeTools(GetType().Assembly, true);
            container.AddTimeProvider();
            container.AddJsonSerializers();
            container.AddAssemblyLoader();
            container.AddResourceLoader();
            container.AddArguments();
            container.AddLogging(route => route.UseConsole());
            container.AddMapper();

            RegisterCommands(container);

            container.Add<ILoader, Loader>().Singleton();
            container.Add<IParser, Parser>().Singleton();
            container.Add<IProcessor, Processor>().Singleton();
            container.Add<ITemplateWriter, TemplateWriter>().Singleton();
            container.Add<IWriter, Writer>().Singleton();
        }

        private void RegisterCommands(IServiceContainer container)
        {
            container.Add<Commands.Group>().AsSelf().Singleton();
            container.Add<Commands.GenerateCommand>().AsSelf().Singleton();
        }
    }
}