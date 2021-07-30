using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Architecture.Base;
using Annium.Data.Operations;

namespace Xws.Models
{
    public class RequestResponseHandlerModel : IHandlerModel
    {
        public Namespace Namespace { get; }
        public string Name { get; }

        public Type[] References => new[]
        {
            typeof(Task<>).MakeGenericType(typeof(IStatusResult<,>).MakeGenericType(typeof(OperationStatus), Response)),
            typeof(CancellationToken),
            Request
        };

        public Type Request { get; }
        public Type Response { get; }

        public RequestResponseHandlerModel(
            Namespace @namespace,
            string name,
            Type request,
            Type response
        )
        {
            Namespace = @namespace;
            Name = name;
            Request = request;
            Response = response;
        }
    }
}