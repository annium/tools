using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.Mediator;
using Annium.Extensions.Jobs;
using Annium.Storage.Abstractions;
using Backuper.Connection.Abstract;
using Backuper.Notification.Abstract;

namespace Backuper.Api.Tests;

// Shared by StateManagerTests and StateControllerTests: both drive the same plan/server graph, one
// through the scheduler and one through HTTP.
internal sealed class TestStorage : IStorage
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

    public Task<Stream> DownloadAsync(string path)
    {
        Operations.Add($"download:{path}");

        return Task.FromResult<Stream>(new MemoryStream("dump"u8.ToArray()));
    }

    public Task<bool> DeleteAsync(string path)
    {
        Operations.Add($"delete:{path}");
        Items.Remove(path);

        return Task.FromResult(true);
    }
}

internal sealed class TestConnection : IConnection
{
    public string? LastPath { get; private set; }
    public string? RestoredPath { get; private set; }
    public int SetupCount { get; private set; }

    private readonly bool _fails;
    private readonly bool _restoreFails;

    public TestConnection(bool fails, bool restoreFails = false)
    {
        _fails = fails;
        _restoreFails = restoreFails;
    }

    public Task SetupAsync()
    {
        SetupCount++;

        return Task.CompletedTask;
    }

    public Task<string> BackupAsync()
    {
        if (_fails)
            throw new InvalidOperationException("backup failed");

        LastPath = Path.GetTempFileName();

        return Task.FromResult(LastPath);
    }

    public Task RestoreAsync(string path)
    {
        RestoredPath = path;

        if (_restoreFails)
            throw new InvalidOperationException("restore failed");

        return Task.CompletedTask;
    }
}

internal sealed class TestChannel : IChannel
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

// the clock advances on every read, so a backup id computed twice yields two different names —
// a fixed clock cannot tell "computed once" from "computed per use"
internal sealed class TestTimeProvider : Annium.ITimeProvider
{
    public static NodaTime.Instant Start { get; } = NodaTime.Instant.FromUtc(2026, 7, 31, 12, 0);

    private readonly bool _advance;
    private int _reads;

    // advancing by default, so a value computed twice differs and "computed once" is observable
    public TestTimeProvider(bool advance = true)
    {
        _advance = advance;
    }

    public NodaTime.Instant Now => _advance ? Start.Plus(NodaTime.Duration.FromMinutes(_reads++)) : Start;

    public DateTime DateTimeNow => Now.ToDateTimeUtc();

    public long UnixMsNow => Now.ToUnixTimeMilliseconds();

    public long UnixSecondsNow => Now.ToUnixTimeSeconds();
}

internal sealed class TestScheduler : IScheduler
{
    public List<string> Intervals { get; } = new();
    public List<Func<Task>> Handlers { get; } = new();

    public IDisposable Schedule(Func<Task> handler, string interval)
    {
        Intervals.Add(interval);
        Handlers.Add(handler);

        return Disposable.Empty;
    }

    internal sealed class Disposable : IDisposable
    {
        public static readonly IDisposable Empty = new Disposable();

        public void Dispose() { }
    }
}

internal sealed class ThrowingChannel : IChannel
{
    public Task InfoAsync(string message) => throw new InvalidOperationException("channel down");

    public Task WarnAsync(string message) => throw new InvalidOperationException("channel down");

    public Task ErrorAsync(string message) => throw new InvalidOperationException("channel down");
}

internal sealed class TestMediator : IMediator
{
    public Task<TResponse> SendAsync<TResponse>(object request, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<TResponse> SendAsync<TResponse>(
        IServiceProvider serviceProvider,
        object request,
        CancellationToken ct = default
    ) => throw new NotSupportedException();
}

internal sealed class TestServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
