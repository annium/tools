namespace Xws.Views
{
    internal class RequestHandlerView : IHandlerView
    {
        public string RequestType { get; }
        public string RequestName { get; }

        public RequestHandlerView(
            string requestType,
            string requestName
        )
        {
            RequestType = requestType;
            RequestName = requestName;
        }

        public override string ToString() => $"{RequestType} {RequestName}";
    }
}