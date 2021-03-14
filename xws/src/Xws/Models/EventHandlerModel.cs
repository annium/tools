using System;

namespace Xws.Models
{
    public class EventHandlerModel : IHandlerModel
    {
        public Namespace Namespace { get; }
        public string Name { get; }
        public Type[] References => new[] {Message};
        public Type Message { get; }

        public EventHandlerModel(
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