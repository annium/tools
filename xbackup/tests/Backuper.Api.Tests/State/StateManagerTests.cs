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

    private sealed class TestStorage : IStorage
    {
        public List<string> Items { get; }
        public List<string> Operations { get; } = new();

        private readonly bool _failUpload;

        public TestStorage(IEnumerable<string> items, bool failUpload = false)
        {
            Items = items.ToList();
            _failUpload = failUpload;
        }

        public Task<string[]> ListAsync(string prefix = "") => Task.FromResult(Items.ToArray());

        public Task UploadAsync(Stream source, string path)
        {
            if (_failUpload)
                throw new InvalidOperationException("upload failed");

            Operations.Add($"upload:{path}");
            Items.Add(path);

            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string path) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(string path)
        {
            Operations.Add($"delete:{path}");
            Items.Remove(path);

            return Task.FromResult(true);
        }
    }

    private sealed class TestConnection : IConnection
    {
        public string? LastPath { get; private set; }

        private readonly bool _fails;

        public TestConnection(bool fails)
        {
            _fails = fails;
        }

        public Task SetupAsync() => Task.CompletedTask;

        public Task<string> BackupAsync()
        {
            if (_fails)
                throw new InvalidOperationException("backup failed");

            LastPath = Path.GetTempFileName();

            return Task.FromResult(LastPath);
        }

        public Task RestoreAsync(string path) => Task.CompletedTask;
    }

    private sealed class TestChannel : IChannel
    {
        public List<string> Infos { get; } = new();
        public List<string> Errors { get; } = new();

        public Task InfoAsync(string message)
        {
            Infos.Add(message);

            return Task.CompletedTask;
        }

        public Task WarnAsync(string message) => Task.CompletedTask;

        public Task ErrorAsync(string message)
        {
            Errors.Add(message);

            return Task.CompletedTask;
        }
    }

    private sealed class TestTimeProvider : Annium.ITimeProvider
    {
        public NodaTime.Instant Now { get; } = NodaTime.Instant.FromUtc(2026, 7, 31, 12, 0);

        public DateTime DateTimeNow => Now.ToDateTimeUtc();

        public long UnixMsNow => Now.ToUnixTimeMilliseconds();

        public long UnixSecondsNow => Now.ToUnixTimeSeconds();
    }

    private sealed class TestScheduler : IScheduler
    {
        public IDisposable Schedule(Func<Task> handler, string interval) => Disposable.Empty;

        private sealed class Disposable : IDisposable
        {
            public static readonly IDisposable Empty = new Disposable();

            public void Dispose() { }
        }
    }
}
