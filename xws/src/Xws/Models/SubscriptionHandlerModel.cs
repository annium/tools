using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Architecture.Base;
using Annium.Data.Operations;

namespace Xws.Models
{
    public class SubscriptionHandlerModel : IHandlerModel
    {
        public Namespace Namespace { get; }
        public string Name { get; }

        public Type[] References => new[]
        {
            Init,
            typeof(CancellationToken),
            typeof(Task<>).MakeGenericType(
                typeof(IStatusResult<,>).MakeGenericType(
                    typeof(OperationStatus),
                    typeof(IAsyncDisposableObservable<>).MakeGenericType(Message)
                )
            )
        };

        public Type Init { get; }
        public Type Message { get; }

        public SubscriptionHandlerModel(
            Namespace @namespace,
            string name,
            Type init,
            Type message
        )
        {
            Namespace = @namespace;
            Name = name;
            Init = init;
            Message = message;
        }
    }
}