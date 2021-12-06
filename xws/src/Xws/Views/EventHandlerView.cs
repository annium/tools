namespace Xws.Views;

internal class EventHandlerView : IHandlerView
{
    public string MessageType { get; }
    public string MessageName { get; }

    public EventHandlerView(
        string messageType,
        string messageName
    )
    {
        MessageType = messageType;
        MessageName = messageName;
    }

    public override string ToString() => $"{MessageType} {MessageName}";
}