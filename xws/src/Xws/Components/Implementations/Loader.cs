using System.IO;
using Annium.Core.Runtime.Loader;
using Annium.Core.Runtime.Types;
using Annium.Logging.Abstractions;
using Xws.Models;

namespace Xws.Components.Implementations
{
    internal class Loader : ILoader
    {
        private readonly IAssemblyLoaderBuilder _assemblyLoaderBuilder;
        private readonly IParser _parser;
        private readonly ILogger<Loader> _logger;

        public Loader(
            IAssemblyLoaderBuilder assemblyLoaderBuilder,
            IParser parser,
            ILogger<Loader> logger
        )
        {
            _assemblyLoaderBuilder = assemblyLoaderBuilder;
            _parser = parser;
            _logger = logger;
        }

        public ApiModel Load(string assemblyPath, string projectName)
        {
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException($"Assembly file '{assemblyPath}' missing.");

            var loader = _assemblyLoaderBuilder.UseFileSystemLoader(Path.GetDirectoryName(assemblyPath)!).Build();
            var name = Path.GetFileNameWithoutExtension(assemblyPath);

            _logger.Info($"load assembly {name}");
            var assembly = loader.Load(name);
            _logger.Info($"get assembly {name} TypeManager");
            var tm = TypeManager.GetInstance(assembly, false);
            _logger.Info($"parse assembly {name} model");
            var model = _parser.Parse(assembly, projectName, tm);
            _logger.Info($"parsed assembly {name}");

            return model;
        }
    }
}