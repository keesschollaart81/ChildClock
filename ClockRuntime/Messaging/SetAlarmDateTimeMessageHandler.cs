using ClockRuntime.Services;

namespace ClockRuntime.Messaging
{
    internal class SetAlarmDateTimeMessageHandler : IHandleMessage<SetAlarmDateTimeMessage>
    {
        private readonly AlarmService _alarmService;

        public SetAlarmDateTimeMessageHandler(AlarmService alarmService)
        {
            _alarmService = alarmService;
        }

        public void Handle(SetAlarmDateTimeMessage msg)
        {
            _alarmService.SetAlarmDateTime(msg.DateTime);
        }
    }
}