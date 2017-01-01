namespace ClockRuntime.Messaging
{
    public sealed class RawIoTHubmessage
    {
        public string BodyType { get; }
        public string Body { get; }

        public RawIoTHubmessage(string type, string body)
        {
            BodyType = type;
            Body = body;
        }
    }
}