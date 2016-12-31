using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.IoT.Lightning.Providers;
using Windows.System.Threading;
using Windows.Devices.Pwm;
using MemBus;
using MemBus.Configurators;
using SimpleInjector;
using Newtonsoft.Json;

namespace DeviceRuntime
{
    public sealed class StartupTask : IBackgroundTask
    {
        private IotHubService _iotHubService;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            if (!LightningProvider.IsLightningEnabled) return;

            var deferral = taskInstance.GetDeferral();

            try
            {
                _iotHubService = new IotHubService();

                var container = new IocContainer();
                var _bus = BusSetup
                    .StartWith<Conservative>()
                    .Apply<IoCSupport>(s => s.SetAdapter(container).SetHandlerInterface(typeof(IHandleMessage<>)))
                    .Construct();

                container.RegisterSingleton(_bus);

                await _iotHubService.ReceiveAndPublishMessages(_bus);

                deferral.Complete();
            }
            catch (Exception ex)
            {
                Debug.Write($"Fatal:{ex}");
                Debugger.Break();
            }
        }

    }

    internal class GpioService
    {
        private Dictionary<int, PwmPin> pwmPins = new Dictionary<int, PwmPin>();

        public async Task SetCyclePercentage(int pinId, double cyclePercentage)
        {
            if (cyclePercentage > 1.0 || cyclePercentage < 0.0) throw new ArgumentOutOfRangeException(nameof(cyclePercentage));

            var controllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
            lock (this)
            {
                var pwmController = controllers[1];
                pwmController.SetDesiredFrequency(50);

                if (!pwmPins.ContainsKey(pinId)) pwmPins.Add(pinId, pwmController.OpenPin(pinId));

                var pin = pwmPins[pinId];
                pin.SetActiveDutyCyclePercentage(cyclePercentage);
                pin.Start();
            }
        }

        ~GpioService() //IDisposable?
        {
            foreach (var pin in pwmPins)
            {
                pin.Value.Stop();
            }
        }
    }

    internal class IotHubService
    {
        private readonly DeviceClient _deviceClient;

        public IotHubService()
        {
            _deviceClient = DeviceClient.Create("smart-clock-iothub-d.azure-devices.net", AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey("jasmijn-1", ""), TransportType.Http1);
        }

        public async Task ReceiveAndPublishMessages(IBus bus)
        {
            while (true)
            {
                try
                {
                    var message = await _deviceClient.ReceiveAsync(); //todo check with http?
                    if (message == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    var messageAsString = Encoding.ASCII.GetString(message.GetBytes());
                    try
                    {
                        var type = message.Properties["BodyType"];
                        bus.Publish(new RawIoTHubmessage(type, messageAsString));
                    }
                    catch (Exception ex)
                    {
                        //whatever
                    }
                    finally
                    {
                        await _deviceClient.CompleteAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

    }

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

    internal interface IHandleMessage<in T>
    {
        void Handle(T msg);
    }

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

    internal class AlarmService
    {
        private DateTime? currentAlarmDateTime;
        private readonly ThreadPoolTimer _timer;
        private readonly GpioService _gpioService;

        public AlarmService(GpioService gpioService)
        {
            _timer = ThreadPoolTimer.CreatePeriodicTimer(TimerTick, TimeSpan.FromMilliseconds(20000));
            _gpioService = gpioService;
        }

        private void TimerTick(ThreadPoolTimer timer)
        {
            if (!currentAlarmDateTime.HasValue) return;

            var minutesLeftToAlarm = (currentAlarmDateTime.Value - DateTime.Now).TotalMinutes;

            var newCyclePercentage = 0.0;
            if (minutesLeftToAlarm < 60 && minutesLeftToAlarm > 0) newCyclePercentage = minutesLeftToAlarm / 60;

            _gpioService.SetCyclePercentage(5, newCyclePercentage).Wait();

            Debug.WriteLine($"Minutes left: {minutesLeftToAlarm}, Percentage: {newCyclePercentage}");
        }

        public void SetAlarmDateTime(DateTime dateTime)
        {
            currentAlarmDateTime = dateTime;
        }
    }

    internal class IocContainer : IocAdapter
    {
        public Container _container { get; private set; }

        public IocContainer()
        {
            _container = new Container();
            _container.RegisterCollection(typeof(IHandleMessage<>), new[] {
                typeof(RawIoTHubmessageHandler),
                typeof(SetAlarmDateTimeMessageHandler)
            });
            _container.RegisterSingleton<AlarmService>();
            _container.RegisterSingleton<GpioService>();
        }

        public void RegisterSingleton<T>(T o) where T : class
        {
            _container.RegisterSingleton(o);
        }
        public IEnumerable<object> GetAllInstances(Type desiredType)
        {
            return _container.GetAllInstances(desiredType);
        }
    }

    internal class SetAlarmDateTimeMessage
    {
        public DateTime DateTime { get; set; }
    }

}
