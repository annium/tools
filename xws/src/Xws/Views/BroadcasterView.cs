namespace Xws.Views;

internal class BroadcasterView : IHandlerView
{
    public string MessageType { get; }
    public string MessageName { get; }

    public BroadcasterView(string messageType, string messageName)
    {
        MessageType = messageType;
        MessageName = messageName;
    }

    public override string ToString() => $"{MessageType} {MessageName}";
}
