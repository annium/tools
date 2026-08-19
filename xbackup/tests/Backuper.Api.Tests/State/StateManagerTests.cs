using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annium.Extensions.Jobs;
using Annium.Logging;
using Annium.Storage.Abstractions;
using Annium.Testing;
using Backuper.Api.State;
using Backuper.Api.Tools;
using Backuper.Connection.Abstract;
using Backuper.Notification.Abstract;
using Xunit;

namespace Backuper.Api.Tests.State;

public class StateManagerTests
{
    [Fact]
    public async Task BackupAsync_Succeeds_UploadsBeforePruning()
    {
        // arrange — regression: pruning ran first, so a failing backup still consumed a slot and
        // the archive drained to nothing over Capacity runs
        var storage = new TestStorage(["2026.07.29_00.00.dump", "2026.07.30_00.00.dump"]);
        var (manager, server, plan) = Setup(storage, capacity: 2);

        // act
        await manager.BackupAsync(server, plan);

        // assert
        storage.Operations.First().StartsWith("upload:").IsTrue();
        storage.Operations.Count(x => x.StartsWith("delete:")).Is(1);
        storage.Operations.Last().Is("delete:2026.07.29_00.00.dump");
    }

    [Fact]
    public async Task BackupAsync_BackupFails_KeepsExistingArchive()
    {
        // arrange
        var storage = new TestStorage(["2026.07.29_00.00.dump", "2026.07.30_00.00.dump"]);
        var (manager, server, plan) = Setup(storage, capacity: 2, connectionFails: true);

        // act — failures are reported through notifications, not rethrown
        await manager.BackupAsync(server, plan);

        // assert — nothing was pruned
        storage.Operations.IsEmpty();
        storage.Items.Has(2);
    }

    [Fact]
    public async Task BackupAsync_UploadFails_DeletesTempFileAndKeepsArchive()
    {
        // arrange
        var storage = new TestStorage(["2026.07.29_00.00.dump"], failUpload: true);
        var (manager, server, plan, connection) = SetupWithConnection(storage, capacity: 2);

        // act
        await manager.BackupAsync(server, plan);

        // assert — the temp file produced by the connection must not leak
        File.Exists(connection.LastPath!).IsFalse();
        storage.Operations.Count(x => x.StartsWith("delete:")).Is(0);
    }

    [Fact]
    public async Task BackupAsync_WithinCapacity_PrunesNothing()
    {
        // arrange
        var storage = new TestStorage(["2026.07.30_00.00.dump"]);
        var (manager, server, plan) = Setup(storage, capacity: 5);

        // act
        await manager.BackupAsync(server, plan);

        // assert
        storage.Operations.Count(x => x.StartsWith("delete:")).Is(0);
    }

    [Fact]
    public async Task BackupAsync_Failure_NotifiesChannels()
    {
        // arrange
        var storage = new TestStorage([]);
        var channel = new TestChannel();
        var (manager, server, plan) = Setup(storage, capacity: 2, connectionFails: true, channel: channel);

        // act
        await manager.BackupAsync(server, plan);

        // assert
        channel.Errors.Has(1);
        channel.Infos.IsEmpty();
    }

    [Fact]
    public async Task BackupAsync_Succeeds_StoresAndAnnouncesTheSameBackupId()
    {
        // arrange — regression: the id was computed once for the message and again for the upload, so
        // the two disagreed whenever the clock crossed a minute between them
        var storage = new TestStorage([]);
        var channel = new TestChannel();
        var (manager, server, plan) = Setup(storage, capacity: 2, channel: channel);

        // act
        await manager.BackupAsync(server, plan);

        // assert
        var uploaded = storage.Operations.Single(x => x.StartsWith("upload:"))["upload:".Length..];
        channel.Infos.Has(1).At(0).IsContaining(uploaded);
    }

    [Fact]
    public async Task SetStateAsync_SchedulesEveryPlanAndSetsUpEveryConnection()
    {
        // arrange — the only path from configured Interval to an actual scheduled call; a deployment
        // backs up several servers, so both loops have to fan out
        var scheduler = new TestScheduler();
        var manager = new StateManager(scheduler, new Namer(new TestTimeProvider()), VoidLogger.Instance);
        var first = new TestConnection(fails: false);
        var second = new TestConnection(fails: false);
        var state = new Backuper.Api.State.State(
            new Dictionary<string, Server>
            {
                ["db"] = new(
                    "db",
                    first,
                    new Dictionary<string, Plan>
                    {
                        ["daily"] = NewPlan("daily", new TestStorage([]), "0 0 * * *"),
                        ["hourly"] = NewPlan("hourly", new TestStorage([]), "0 * * * *"),
                    }
                ),
                ["other"] = new(
                    "other",
                    second,
                    new Dictionary<string, Plan> { ["weekly"] = NewPlan("weekly", new TestStorage([]), "0 0 * * 0") }
                ),
            }
        );

        // act
        await manager.SetStateAsync(state);

        // assert
        first.SetupCount.Is(1);
        second.SetupCount.Is(1);
        scheduler.Intervals.Has(3);
        scheduler.Intervals.Contains("0 0 * * *").IsTrue();
        scheduler.Intervals.Contains("0 * * * *").IsTrue();
        scheduler.Intervals.Contains("0 0 * * 0").IsTrue();
    }

    [Fact]
    public async Task BackupAsync_Failure_NotifiesEveryChannel()
    {
        // arrange — a plan can carry several channels, and every other case wires exactly one
        var storage = new TestStorage([]);
        var slack = new TestChannel();
        var email = new TestChannel();
        var manager = new StateManager(new TestScheduler(), new Namer(new TestTimeProvider()), VoidLogger.Instance);
        var plan = new Plan(
            "daily",
            storage,
            "0 0 * * *",
            2,
            new Dictionary<string, IChannel> { ["slack"] = slack, ["email"] = email }
        );
        var server = new Server(
            "db",
            new TestConnection(fails: true),
            new Dictionary<string, Plan> { ["daily"] = plan }
        );

        // act
        await manager.BackupAsync(server, plan);

        // assert
        slack.Errors.Has(1);
        email.Errors.Has(1);
    }

    [Fact]
    public async Task SetStateAsync_CalledTwice_Throws()
    {
        // arrange — a second state would schedule a second set of jobs against the first one's plans
        var manager = new StateManager(new TestScheduler(), new Namer(new TestTimeProvider()), VoidLogger.Instance);
        var state = new Backuper.Api.State.State(new Dictionary<string, Server>());
        await manager.SetStateAsync(state);

        // act
        var exception = await Wrap.It(async () => await manager.SetStateAsync(state))
            .ThrowsAsync<InvalidOperationException>();

        // assert
        exception.Message.Is("State is already set");
    }

    [Fact]
    public async Task SetStateAsync_ScheduledJob_RunsTheBackup()
    {
        // arrange — BackupAsync is internal, so the scheduled delegate is what actually connects a plan
        // to a backup run
        var scheduler = new TestScheduler();
        var manager = new StateManager(scheduler, new Namer(new TestTimeProvider()), VoidLogger.Instance);
        var storage = new TestStorage([]);
        var plans = new Dictionary<string, Plan> { ["daily"] = NewPlan("daily", storage, "0 0 * * *") };
        var state = new Backuper.Api.State.State(
            new Dictionary<string, Server> { ["db"] = new("db", new TestConnection(fails: false), plans) }
        );
        await manager.SetStateAsync(state);

        // act
        await scheduler.Handlers.Single()();

        // assert
        storage.Operations.Count(x => x.StartsWith("upload:")).Is(1);
    }

    [Fact]
    public async Task BackupAsync_ChannelThrows_DoesNotEscapeAndStillNotifiesTheOthers()
    {
        // arrange — BackupAsync is what the scheduler awaits, and an exception leaving it kills that
        // plan's recurring loop until the process restarts
        var storage = new TestStorage([]);
        var working = new TestChannel();
        var manager = new StateManager(new TestScheduler(), new Namer(new TestTimeProvider()), VoidLogger.Instance);
        var plan = new Plan(
            "daily",
            storage,
            "0 0 * * *",
            2,
            new Dictionary<string, IChannel> { ["broken"] = new ThrowingChannel(), ["slack"] = working }
        );
        var server = new Server(
            "db",
            new TestConnection(fails: false),
            new Dictionary<string, Plan> { ["daily"] = plan }
        );

        // act
        var exception = await Record.ExceptionAsync(() => manager.BackupAsync(server, plan));

        // assert
        exception.IsNull();
        working.Infos.Has(1);
    }

    private static Plan NewPlan(string name, TestStorage storage, string interval) =>
        new(name, storage, interval, 2, new Dictionary<string, IChannel> { ["slack"] = new TestChannel() });

    private static (StateManager Manager, Server Server, Plan Plan) Setup(
        TestStorage storage,
        int capacity,
        bool connectionFails = false,
        TestChannel? channel = null
    )
    {
        var (manager, server, plan, _) = SetupWithConnection(storage, capacity, connectionFails, channel);

        return (manager, server, plan);
    }

    private static (StateManager Manager, Server Server, Plan Plan, TestConnection Connection) SetupWithConnection(
        TestStorage storage,
        int capacity,
        bool connectionFails = false,
        TestChannel? channel = null
    )
    {
        var manager = new StateManager(new TestScheduler(), new Namer(new TestTimeProvider()), VoidLogger.Instance);
        var connection = new TestConnection(connectionFails);
        var plan = new Plan(
            "daily",
            storage,
            "0 0 * * *",
            capacity,
            new Dictionary<string, IChannel> { ["slack"] = channel ?? new TestChannel() }
        );
        var server = new Server("db", connection, new Dictionary<string, Plan> { ["daily"] = plan });

        return (manager, server, plan, connection);
    }
}
