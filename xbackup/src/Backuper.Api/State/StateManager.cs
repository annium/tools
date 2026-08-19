using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annium.Extensions.Jobs;
using Annium.Logging;
using Backuper.Api.Tools;
using Backuper.Notification.Abstract;

namespace Backuper.Api.State;

public class StateManager : ILogSubject
{
    public ILogger Logger { get; }
    private readonly IScheduler _scheduler;
    private readonly Namer _namer;

    public State? State { get; private set; }

    public StateManager(IScheduler scheduler, Namer namer, ILogger logger)
    {
        _scheduler = scheduler;
        _namer = namer;
        Logger = logger;
    }

    public Task SetStateAsync(State state)
    {
        if (State != null)
            throw new InvalidOperationException("State is already set");

        State = state;

        return StartAsync();
    }

    private async Task StartAsync()
    {
        this.Debug("StateManager starting");

        this.Debug("Setup connections");
        var connections = State!.Servers.Values.Select(s => s.Connection).ToArray();
        await Task.WhenAll(connections.Select(s => s.SetupAsync()));

        this.Debug("Schedule operations");
        foreach (var server in State.Servers.Values)
        foreach (var plan in server.Plans.Values)
            _scheduler.Schedule(() => BackupAsync(server, plan), plan.Interval);
    }

    internal async Task BackupAsync(Server server, Plan plan)
    {
        var backupId = _namer.GetName();
        try
        {
            // create backup
            var path = await server.Connection.BackupAsync();

            // upload backup, then drop the temp file whatever the outcome
            try
            {
                using var fs = File.OpenRead(path);
                await plan.Storage.UploadAsync(fs, backupId);
            }
            finally
            {
                File.Delete(path);
            }

            // cleanup happens only after the new backup is stored: pruning first meant a failing
            // backup still consumed a slot, draining the archive to nothing over Capacity runs
            var obsoleteItems = (await plan.Storage.ListAsync())
                .OrderByDescending(i => i)
                .Skip(plan.Capacity)
                .ToArray();
            foreach (var item in obsoleteItems)
                await plan.Storage.DeleteAsync(item);

            await plan.NotifyAllAsync(
                this,
                ch => ch.InfoAsync($"{server} {plan}: scheduled backup {backupId} procedure succeed")
            );
        }
        catch (Exception e)
        {
            this.Error(e);
            await plan.NotifyAllAsync(
                this,
                ch => ch.ErrorAsync($"{server} {plan}: scheduled backup {backupId} procedure failed: {e}")
            );
        }
    }
}
