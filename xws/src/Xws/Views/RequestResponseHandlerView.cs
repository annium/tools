namespace Xws.Views
{
    internal class RequestResponseHandlerView : IHandlerView
    {
        public string RequestType { get; }
        public string RequestName { get; }
        public string ResponseType { get; }

        public RequestResponseHandlerView(
            string requestType,
            string requestName,
            string responseType
        )
        {
            RequestType = requestType;
            RequestName = requestName;
            ResponseType = responseType;
        }

        public override string ToString() => $"{RequestType} {RequestName} -> {ResponseType}";
    }
}