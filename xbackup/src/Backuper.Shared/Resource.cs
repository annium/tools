using System;
using System.Threading.Tasks;
using Annium.Logging;

namespace Backuper.Shared;

public abstract class Resource<T> : ILogSubject
    where T : class
{
    public ILogger Logger { get; }
    protected T Entity { get; }

    // every proxy passes what it wraps — "Connection"/"postgres", "Channel"/"slack" — and without it
    // every line below reads the same, so a deployment backing up several servers cannot tell whose
    // operation failed
    private readonly string _resource;

    protected Resource(T entity, string category, string type, ILogger logger)
    {
        Entity = entity;
        Logger = logger;
        _resource = $"{category} {type}";
    }

    protected async Task<TResult> SafeAsync<TResult>(string operation, Func<Task<TResult>> handleAsync)
    {
        try
        {
            this.Debug<string, string>("{resource} {operation} start", _resource, operation);
            var result = await handleAsync();
            this.Debug<string, string>("{resource} {operation} succeed", _resource, operation);

            return result;
        }
        catch
        {
            this.Debug<string, string>("{resource} {operation} failed", _resource, operation);
            throw;
        }
    }

    protected async Task SafeAsync(string operation, Func<Task> handleAsync)
    {
        try
        {
            this.Debug<string, string>("{resource} {operation} start", _resource, operation);
            await handleAsync();
            this.Debug<string, string>("{resource} {operation} succeed", _resource, operation);
        }
        catch
        {
            this.Debug<string, string>("{resource} {operation} failed", _resource, operation);
            throw;
        }
    }
}
