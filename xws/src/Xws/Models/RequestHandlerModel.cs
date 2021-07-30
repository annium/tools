using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Architecture.Base;
using Annium.Data.Operations;

namespace Xws.Models
{
    public class RequestHandlerModel : IHandlerModel
    {
        public Namespace Namespace { get; }
        public string Name { get; }
        public Type[] References => new[]
        {
            typeof(Task<IStatusResult<OperationStatus>>),
            typeof(CancellationToken),
            Request
        };
        public Type Request { get; }

        public RequestHandlerModel(
            Namespace @namespace,
            string name,
            Type request
        )
        {
            Namespace = @namespace;
            Name = name;
            Request = request;
        }
    }
}