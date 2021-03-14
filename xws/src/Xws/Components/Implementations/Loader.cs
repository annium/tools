using System.IO;
using Annium.Core.Runtime.Loader;
using Annium.Core.Runtime.Types;
using Xws.Models;

namespace Xws.Components.Implementations
{
    internal class Loader : ILoader
    {
        private readonly IAssemblyLoaderBuilder _assemblyLoaderBuilder;
        private readonly IParser _parser;

        public Loader(
            IAssemblyLoaderBuilder assemblyLoaderBuilder,
            IParser parser
        )
        {
            _assemblyLoaderBuilder = assemblyLoaderBuilder;
            _parser = parser;
        }

        public ApiModel Load(string assemblyPath, string projectName)
        {
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException($"Assembly file '{assemblyPath}' missing.");

            var loader = _assemblyLoaderBuilder.UseFileSystemLoader(Path.GetDirectoryName(assemblyPath)!).Build();
            var name = Path.GetFileNameWithoutExtension(assemblyPath);

            var assembly = loader.Load(name);
            var tm = TypeManager.GetInstance(assembly, false);
            var model = _parser.Parse(assembly, projectName, tm);

            return model;
        }
    }
}