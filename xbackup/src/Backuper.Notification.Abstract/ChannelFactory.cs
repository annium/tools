using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace Backuper.Notification.Abstract;

public class ChannelFactory
{
    private readonly IServiceProvider _provider;

    public ChannelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IChannel CreateChannel(ConfigurationBase configuration)
    {
        var factoryType = typeof(Func<,>).MakeGenericType(configuration.GetType(), typeof(IChannel));

        var factory = (Delegate)_provider.GetRequiredService(factoryType);

        try
        {
            // the registered factory is a Func<TConfiguration, IChannel>, so it never returns null
            var channel = (IChannel)factory.DynamicInvoke(configuration)!;

            return channel;
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
