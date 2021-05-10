using System.IO;
using System.Threading;
using Annium.Extensions.Arguments;
using Annium.Logging.Abstractions;
using Xws.Components;
using Xws.Models;

namespace Xws.Commands
{
    internal class GenerateCommand : Command<GenerateCommandConfiguration>
    {
        public override string Id { get; } = "gen";
        public override string Description { get; } = "generate client";
        private readonly ILoader _loader;
        private readonly IProcessor _processor;
        private readonly IWriter _writer;
        private readonly ILogger<GenerateCommand> _logger;

        public GenerateCommand(
            ILoader loader,
            IProcessor processor,
            IWriter writer,
            ILogger<GenerateCommand> logger
        )
        {
            _loader = loader;
            _processor = processor;
            _writer = writer;
            _logger = logger;
        }

        public override void Handle(GenerateCommandConfiguration cfg, CancellationToken ct)
        {
            _logger.Info($"Generate '{cfg.ProjectName}' client");

            _logger.Info($"Load '{cfg.ProjectName}' model");
            var model = _loader.Load(cfg.Assembly, cfg.ProjectName);

            _logger.Info("Process api model to api view");
            var ns = Namespace.New(string.IsNullOrWhiteSpace(cfg.Namespace) ? Path.GetFileName(cfg.Output) : cfg.Namespace);
            var view = _processor.Process(ns, model);

            _logger.Info($"Write api view to {cfg.Output}");
            _writer.Write(cfg.Output, view);
            _logger.Info($"Client written to {cfg.Output}");
        }
    }

    internal class GenerateCommandConfiguration
    {
        [Option("a", true)]
        [Help("Path to API assembly.")]
        public string Assembly
        {
            get => _assembly;
            set => _assembly = Path.GetFullPath(value);
        }

        [Option("n", true)]
        [Help("Project name.")]
        public string ProjectName { get; private set; } = string.Empty;

        [Option("o", true)]
        [Help("Output directory. Will be removed if exists.")]
        public string Output
        {
            get => _output;
            set => _output = Path.GetFullPath(value);
        }

        [Option("ns")]
        [Help("Root namespace for created classes.")]
        public string Namespace { get; set; } = string.Empty;

        private string _assembly = string.Empty;
        private string _output = string.Empty;
    }
}