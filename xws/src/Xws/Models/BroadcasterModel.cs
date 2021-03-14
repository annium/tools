using System;

namespace Xws.Models
{
    public class BroadcasterModel : IHandlerModel
    {
        public Namespace Namespace { get; }
        public string Name { get; }
        public Type[] References => new[] {typeof(Action), typeof(Action<>).MakeGenericType(Message)};
        public Type Message { get; }

        public BroadcasterModel(
            Namespace @namespace,
            string name,
            Type message
        )
        {
            Namespace = @namespace;
            Name = name;
            Message = message;
        }
    }
}