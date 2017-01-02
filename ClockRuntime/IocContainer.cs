using System;
using System.Collections.Generic;
using ClockRuntime.Messaging;
using ClockRuntime.Services;
using MemBus;
using SimpleInjector;

namespace ClockRuntime
{
    internal class IocContainer : IocAdapter, IDisposable
    {
        public Container Container { get; private set; }

        public IocContainer()
        {
            Container = new Container();
            Container.RegisterCollection(typeof(IHandleMessage<>), new[] {
                typeof(RawIoTHubmessageHandler),
                typeof(SetAlarmDateTimeMessageHandler)
            });
            Container.RegisterSingleton<AlarmService>();
            Container.RegisterSingleton<GpioService>();
            Container.RegisterSingleton<SettingsProvider>(); 
            Container.RegisterSingleton<IotHubService>();
        }

        public void RegisterSingleton<T>(T o) where T : class
        {
            Container.RegisterSingleton(o);
        }

        public T GetInstance<T>()
        {
            return (T)Container.GetInstance(typeof(T));
        }

        public IEnumerable<object> GetAllInstances(Type desiredType)
        {
            return Container.GetAllInstances(desiredType);
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}