using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace Backuper.Connection.Abstract;

public class ConnectionFactory
{
    private readonly IServiceProvider _provider;

    public ConnectionFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IConnection CreateConnection(ConfigurationBase configuration)
    {
        var factoryType = typeof(Func<,>).MakeGenericType(configuration.GetType(), typeof(IConnection));

        var factory = (Delegate)_provider.GetRequiredService(factoryType);

        try
        {
            // the registered factory is a Func<TConfiguration, IConnection>, so it never returns null
            var connection = (IConnection)factory.DynamicInvoke(configuration)!;

            return connection;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // rethrown through ExceptionDispatchInfo so the failure keeps the stack trace of where it
            // actually happened, inside the factory, rather than pointing here
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

            throw;
        }
    }
}
