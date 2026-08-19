using System;
using System.Threading.Tasks;
using Annium.Logging;
using Annium.Testing;
using Backuper.Shared;
using Xunit;

namespace Backuper.Api.Tests.Shared;

// Every connection, channel and storage reaches its entity through Resource<T>.SafeAsync, and its only
// job beyond logging is to let the failure through: StateManager.BackupAsync decides between the
// success notification and the failure one by catching what comes out of here. A SafeAsync that
// swallowed instead of rethrowing would report every failed backup as succeeded.
public class ResourceTests
{
    [Fact]
    public async Task SafeAsync_WithResult_ReturnsIt()
    {
        // arrange
        var resource = new TestResource();

        // act
        var result = await resource.RunAsync(() => Task.FromResult(42));

        // assert
        result.Is(42);
    }

    [Fact]
    public async Task SafeAsync_WithResult_RethrowsTheFailure()
    {
        // arrange
        var resource = new TestResource();

        // act
        var exception = await Wrap.It(async () =>
                await resource.RunAsync<int>(() => throw new InvalidOperationException("boom"))
            )
            .ThrowsAsync<InvalidOperationException>();

        // assert
        exception.Message.Is("boom");
    }

    [Fact]
    public async Task SafeAsync_WithoutResult_RunsTheOperation()
    {
        // arrange
        var resource = new TestResource();
        var ran = false;

        // act
        await resource.RunAsync(() =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        // assert
        ran.IsTrue();
    }

    [Fact]
    public async Task SafeAsync_WithoutResult_RethrowsTheFailure()
    {
        // arrange
        var resource = new TestResource();

        // act
        var exception = await Wrap.It(async () =>
                await resource.RunAsync(() => throw new InvalidOperationException("boom"))
            )
            .ThrowsAsync<InvalidOperationException>();

        // assert
        exception.Message.Is("boom");
    }

    private sealed class TestResource : Resource<object>
    {
        public TestResource()
            : base(new object(), "test", "test", VoidLogger.Instance) { }

        public Task<TResult> RunAsync<TResult>(Func<Task<TResult>> handleAsync) => SafeAsync("op", handleAsync);

        public Task RunAsync(Func<Task> handleAsync) => SafeAsync("op", handleAsync);
    }
}
