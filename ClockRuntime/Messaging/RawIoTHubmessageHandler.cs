using System;
using MemBus;
using Newtonsoft.Json;

namespace ClockRuntime.Messaging
{
    internal class RawIoTHubmessageHandler : IHandleMessage<RawIoTHubmessage>
    {
        private readonly IBus _bus;
        public RawIoTHubmessageHandler(IBus bus)
        {
            _bus = bus;
        }
        public void Handle(RawIoTHubmessage message)
        {
            switch (message.BodyType)
            {
                case "SetAlarmDateTime":
                    _bus.Publish(JsonConvert.DeserializeObject<SetAlarmDateTimeMessage>(message.Body));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}