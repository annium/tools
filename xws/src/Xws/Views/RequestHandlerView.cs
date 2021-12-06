namespace Xws.Views;

internal class RequestHandlerView : IHandlerView
{
    public string RequestType { get; }
    public bool RequestTypeNullable { get; }
    public string RequestName { get; }

    public RequestHandlerView(
        string requestType,
        bool requestTypeNullable,
        string requestName
    )
    {
        RequestType = requestType;
        RequestTypeNullable = requestTypeNullable;
        RequestName = requestName;
    }

    public override string ToString() => $"{RequestType} {RequestName}";
}