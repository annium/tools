using System.IO;
using System.Threading;
using Annium.Extensions.Arguments;
using Annium.Logging;
using Xws.Components;
using Xws.Models;

namespace Xws.Commands;

internal class GenerateCommand : Command<GenerateCommandConfiguration>, ICommandDescriptor, ILogSubject
{
    public static string Id => "gen";
    public static string Description => "generate client";
    public ILogger Logger { get; }
    private readonly ILoader _loader;
    private readonly IProcessor _processor;
    private readonly IWriter _writer;

    public GenerateCommand(
        ILoader loader,
        IProcessor processor,
        IWriter writer,
        ILogger logger
    )
    {
        _loader = loader;
        _processor = processor;
        _writer = writer;
        Logger = logger;
    }

    public override void Handle(GenerateCommandConfiguration cfg, CancellationToken ct)
    {
        this.Info($"Generate '{cfg.ProjectName}' client");

        this.Info($"Load '{cfg.ProjectName}' model");
        var model = _loader.Load(cfg.Assembly, cfg.ProjectName);

        this.Info("Process api model to api view");
        var ns = Namespace.New(string.IsNullOrWhiteSpace(cfg.Namespace) ? Path.GetFileName(cfg.Output) : cfg.Namespace);
        var view = _processor.Process(ns, model);

        this.Info($"Write api view to {cfg.Output}");
        _writer.Write(cfg.Output, view);
        this.Info($"Client written to {cfg.Output}");
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