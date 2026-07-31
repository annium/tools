using System;
using System.IO;
using System.Threading.Tasks;
using Annium.Extensions.Shell;
using Backuper.Connection.Abstract;
using Npgsql;

namespace Backuper.Connection.PostgreSQL;

public class Connection : IConnection
{
    private readonly Configuration _cfg;
    private readonly IShell _shell;

    public Connection(Configuration cfg, IShell shell)
    {
        _cfg = cfg;
        _shell = shell;
    }

    public async Task SetupAsync()
    {
        await using var conn = new NpgsqlConnection(GetConnectionString());
        await conn.OpenAsync();
    }

    public async Task<string> BackupAsync()
    {
        var path = Path.GetTempFileName();

        try
        {
            var result = await RunAsync($"pg_dump -Fc -v --dbname={GetDbUri()} -f {path}");
            if (!result.IsSuccess)
                throw new InvalidOperationException($"backup of {_cfg.Db} failed");

            return path;
        }
        catch
        {
            // the temp file is created before the dump runs, so a failure must not leak it —
            // this method runs on a schedule and would otherwise fill the temp volume
            File.Delete(path);
            throw;
        }
    }

    public async Task RestoreAsync(string path)
    {
        var result = await RunAsync($"pg_restore -Fc --clean --if-exists -v --dbname={GetDbUri()} {path}");
        if (!result.IsSuccess)
            throw new InvalidOperationException($"restore of {_cfg.Db} failed");
    }

    /// <summary>
    /// Runs a libpq-based command with the password supplied via PGPASSWORD.
    /// The password must stay out of the command line: process arguments are world-readable
    /// (<c>ps</c>) and the command is echoed to the log by <see cref="IShellInstance.Print"/>.
    /// </summary>
    private Task<ShellResult> RunAsync(string command) =>
        _shell.Cmd(command).Configure(x => x.Environment["PGPASSWORD"] = _cfg.Pass).Print(true).RunAsync();

    /// <summary>
    /// Builds the connection URI without credentials; user and database are percent-encoded
    /// so that a value containing <c>@</c>, <c>/</c> or <c>:</c> cannot rewrite the target.
    /// </summary>
    private string GetDbUri() =>
        $"postgresql://{Uri.EscapeDataString(_cfg.User)}@{_cfg.Host}:{_cfg.Port}/{Uri.EscapeDataString(_cfg.Db)}";

    private string GetConnectionString() =>
        string.Join(
            ';',
            $"Host={_cfg.Host}",
            $"Port={_cfg.Port}",
            $"Database={_cfg.Db}",
            $"Username={_cfg.User}",
            $"Password={_cfg.Pass}",
            "SSL Mode=Prefer",
            "Trust Server Certificate=true"
        );
}
