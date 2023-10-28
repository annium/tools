namespace Xws.Views;

internal class RequestResponseHandlerView : IHandlerView
{
    public string RequestType { get; }
    public bool RequestTypeNullable { get; }
    public string RequestName { get; }
    public string ResponseType { get; }

    public RequestResponseHandlerView(
        string requestType,
        bool requestTypeNullable,
        string requestName,
        string responseType
    )
    {
        RequestType = requestType;
        RequestTypeNullable = requestTypeNullable;
        RequestName = requestName;
        ResponseType = responseType;
    }

    public override string ToString() => $"{RequestType} {RequestName} -> {ResponseType}";
}
