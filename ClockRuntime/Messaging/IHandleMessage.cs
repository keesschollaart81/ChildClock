namespace ClockRuntime.Messaging
{
    internal interface IHandleMessage<in T>
    {
        void Handle(T msg);
    }
}