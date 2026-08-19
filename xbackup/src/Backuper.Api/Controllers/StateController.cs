using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Annium.AspNetCore.Extensions;
using Annium.Core.Mediator;
using Annium.Logging;
using Backuper.Api.State;
using Backuper.Api.Tools;
using Backuper.Notification.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Backuper.Api.Controllers;

[Route("/")]
public class StateController : ServerController, ILogSubject
{
    public ILogger Logger { get; }

    private readonly Func<State.State> _getState;

    private readonly Namer _namer;

    public StateController(
        Func<State.State> getState,
        Namer namer,
        IMediator mediator,
        IServiceProvider sp,
        ILogger logger
    )
        : base(mediator, sp)
    {
        _getState = getState;
        _namer = namer;
        Logger = logger;
    }

    [HttpGet("state")]
    public IActionResult GetState()
    {
        return Ok(_getState());
    }

    [HttpGet("{serverName}/backups/{planName}")]
    public async Task<IActionResult> ListBackupsAsync(string serverName, string planName)
    {
        var (_, plan, errorResult) = ResolveServerPlan(serverName, planName);
        if (errorResult != null)
            return errorResult;

        var backups = await plan.Storage.ListAsync();

        return Ok(backups);
    }

    [HttpPost("{serverName}/backups/{planName}")]
    public async Task<IActionResult> CreateBackupAsync(string serverName, string planName)
    {
        var (server, plan, errorResult) = ResolveServerPlan(serverName, planName);
        if (errorResult != null)
            return errorResult;

        var backupId = _namer.GetName();
        try
        {
            await plan.NotifyAllAsync(
                this,
                ch => ch.InfoAsync($"{server} {plan}: start manual backup {backupId} procedure")
            );

            // create backup
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: create backup {backupId}"));
            var path = await server.Connection.BackupAsync();
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} created"));

            // upload backup, then drop the temp file whatever the outcome
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: upload backup {backupId}"));
            try
            {
                using var fs = System.IO.File.OpenRead(path);
                await plan.Storage.UploadAsync(fs, backupId);
            }
            finally
            {
                System.IO.File.Delete(path);
            }
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} uploaded"));

            // cleanup happens only after the new backup is stored, as it does on the scheduled path:
            // pruning first meant a failing backup still consumed a slot
            var deletedItems = (await plan.Storage.ListAsync()).OrderByDescending(i => i).Skip(plan.Capacity).ToArray();
            if (deletedItems.Length > 0)
            {
                await plan.NotifyAllAsync(
                    this,
                    ch => ch.InfoAsync($"{server} {plan}: cleanup {deletedItems.Length} old backups")
                );
                foreach (var item in deletedItems)
                {
                    await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: delete old backup {item}"));
                    await plan.Storage.DeleteAsync(item);
                }
            }
            else
                await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: no cleanup needed"));

            await plan.NotifyAllAsync(
                this,
                ch => ch.InfoAsync($"{server} {plan}: manual backup {backupId} procedure succeed")
            );

            return Ok(backupId);
        }
        catch (Exception e)
        {
            this.Error(e);
            await plan.NotifyAllAsync(
                this,
                ch => ch.ErrorAsync($"{server} {plan}: manual backup {backupId} procedure failed: {e}")
            );

            return new ObjectResult(e.Message) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }
    }

    [HttpPost("{serverName}/backups/{planName}/{backupId}")]
    public async Task<IActionResult> RestoreBackupAsync(string serverName, string planName, string backupId)
    {
        var (server, plan, errorResult) = ResolveServerPlan(serverName, planName);
        if (errorResult != null)
            return errorResult;

        try
        {
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: start restore {backupId} procedure"));

            // ensure backup exists
            var list = await plan.Storage.ListAsync();
            if (!list.Contains(backupId))
            {
                await plan.NotifyAllAsync(
                    this,
                    ch => ch.WarnAsync($"{server} {plan}: backup {backupId} not found in storage")
                );
                return NotFound($"Backup {backupId} not found in storage");
            }

            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} found in storage"));

            // get temp file path
            var path = Path.GetTempFileName();
            System.IO.File.Delete(path);

            // download and restore, then drop the downloaded dump whatever the outcome — a failing
            // restore would otherwise leave a full-sized file behind on every attempt
            try
            {
                await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: download backup {backupId}"));
                using (var ms = await plan.Storage.DownloadAsync(backupId))
                using (var fs = System.IO.File.OpenWrite(path))
                    await ms.CopyToAsync(fs);
                await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} downloaded"));

                await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: restore backup {backupId}"));
                await server.Connection.RestoreAsync(path);
                await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} restored"));
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            await plan.NotifyAllAsync(
                this,
                ch => ch.InfoAsync($"{server} {plan}: restore {backupId} procedure succeed")
            );

            return NoContent();
        }
        catch (Exception e)
        {
            this.Error(e);
            await plan.NotifyAllAsync(this, ch => ch.ErrorAsync($"{server} {plan}: restore procedure failed: {e}"));

            return new ObjectResult(e.Message) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }
    }

    [HttpDelete("{serverName}/backups/{planName}/{backupId}")]
    public async Task<IActionResult> DeleteBackupAsync(string serverName, string planName, string backupId)
    {
        var (server, plan, errorResult) = ResolveServerPlan(serverName, planName);
        if (errorResult != null)
            return errorResult;

        try
        {
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: start delete {backupId} procedure"));

            // ensure backup exists
            var list = await plan.Storage.ListAsync();
            if (!list.Contains(backupId))
            {
                await plan.NotifyAllAsync(
                    this,
                    ch => ch.WarnAsync($"{server} {plan}: backup {backupId} not found in storage")
                );
                return NotFound($"Backup {backupId} not found in storage");
            }

            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} found in storage"));

            // download backup to temp path
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: delete backup {backupId}"));
            await plan.Storage.DeleteAsync(backupId);
            await plan.NotifyAllAsync(this, ch => ch.InfoAsync($"{server} {plan}: backup {backupId} deleted"));

            await plan.NotifyAllAsync(
                this,
                ch => ch.InfoAsync($"{server} {plan}: delete {backupId} procedure succeed")
            );

            return NoContent();
        }
        catch (Exception e)
        {
            this.Error(e);
            await plan.NotifyAllAsync(
                this,
                ch => ch.ErrorAsync($"{server} {plan}: delete {backupId} procedure failed: {e}")
            );

            return new ObjectResult(e.Message) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }
    }

    private (Server, Plan, IActionResult?) ResolveServerPlan(string serverName, string planName)
    {
        var state = _getState();
        // TryGetValue, not the indexer: it throws KeyNotFoundException for an unknown name, so the
        // null checks these guards used to make were dead and an unknown server answered 500, not 404
        if (!state.Servers.TryGetValue(serverName, out var server))
            return (default!, default!, NotFound($"Server {serverName} is not configured"));

        if (!server.Plans.TryGetValue(planName, out var plan))
            return (default!, default!, NotFound($"Server {serverName} has no plan {planName}"));

        return (server, plan, null);
    }
}
