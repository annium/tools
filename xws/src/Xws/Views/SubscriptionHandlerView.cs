namespace Xws.Views;

internal class SubscriptionHandlerView : IHandlerView
{
    public string InitType { get; }
    public bool InitTypeNullable { get; }
    public string InitName { get; }
    public string MessageType { get; }

    public SubscriptionHandlerView(string initType, bool initTypeNullable, string initName, string messageType)
    {
        InitType = initType;
        InitTypeNullable = initTypeNullable;
        InitName = initName;
        MessageType = messageType;
    }

    public override string ToString() => $"{InitType} {InitName} -> {MessageType}";
}
