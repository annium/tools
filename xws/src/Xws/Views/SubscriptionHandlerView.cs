namespace Xws.Views
{
    internal class SubscriptionHandlerView : IHandlerView
    {
        public string InitType { get; }
        public string InitName { get; }
        public string MessageType { get; }

        public SubscriptionHandlerView(
            string initType,
            string initName,
            string messageType
        )
        {
            InitType = initType;
            InitName = initName;
            MessageType = messageType;
        }

        public override string ToString() => $"{InitType} {InitName} -> {MessageType}";
    }
}