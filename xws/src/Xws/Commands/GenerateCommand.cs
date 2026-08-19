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

    public GenerateCommand(ILoader loader, IProcessor processor, IWriter writer, ILogger logger)
    {
        _loader = loader;
        _processor = processor;
        _writer = writer;
        Logger = logger;
    }

    public override void Handle(GenerateCommandConfiguration cfg, CancellationToken ct)
    {
        this.Info<string>("Generate '{projectName}' client", cfg.ProjectName);

        this.Info<string>("Load '{projectName}' model", cfg.ProjectName);
        var model = _loader.Load(cfg.Assembly, cfg.ProjectName);

        this.Info("Process api model to api view");
        var ns = Namespace.New(string.IsNullOrWhiteSpace(cfg.Namespace) ? Path.GetFileName(cfg.Output) : cfg.Namespace);
        var view = _processor.Process(ns, model);

        // the output directory is generated in full, so whatever is there belongs to an older run: a
        // handler renamed or removed since would otherwise keep its generated file, as the help text
        // has always said it would not. Same contract, and same fix, as the xrest client generator
        if (Directory.Exists(cfg.Output))
        {
            this.Info<string>("Remove existing {output}", cfg.Output);
            Directory.Delete(cfg.Output, recursive: true);
        }

        this.Info<string>("Write api view to {output}", cfg.Output);
        _writer.Write(cfg.Output, view);
        this.Info<string>("Client written to {output}", cfg.Output);
    }
}

internal class GenerateCommandConfiguration
{
    [Option("a", true)]
    [Help("Path to API assembly.")]
    public string Assembly
    {
        get;
        set => field = Path.GetFullPath(value);
    } = string.Empty;

    [Option("n", true)]
    [Help("Project name.")]
    public string ProjectName { get; private set; } = string.Empty;

    [Option("o", true)]
    [Help("Output directory. Will be removed if exists.")]
    public string Output
    {
        get;
        set => field = Path.GetFullPath(value);
    } = string.Empty;

    [Option("ns")]
    [Help("Root namespace for created classes.")]
    public string Namespace { get; set; } = string.Empty;
}
