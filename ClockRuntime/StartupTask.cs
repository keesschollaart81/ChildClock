using System;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.Background;
using ClockRuntime.Messaging;
using ClockRuntime.Services;
using Microsoft.IoT.Lightning.Providers;
using MemBus;
using MemBus.Configurators;
using NLog;

namespace ClockRuntime
{
    public sealed class StartupTask : IBackgroundTask
    { 
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            if (!LightningProvider.IsLightningEnabled) return;

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            taskInstance.Canceled += (sender, reason) => tokenSource.Cancel();

            try
            {
                using (var container = new IocContainer())
                using (var iotHubService = new IotHubService(container.GetInstance<SettingsProvider>()))
                {
                    var bus = BusSetup
                        .StartWith<Conservative>()
                        .Apply<IoCSupport>(s => s.SetAdapter(container).SetHandlerInterface(typeof(IHandleMessage<>)))
                        .Construct();

                    container.RegisterSingleton(bus);

                    await iotHubService.ReceiveAndPublishMessages(bus, token);
                }
            }
            catch (Exception ex)
            {
                Debug.Write($"Fatal:{ex}");
                Debugger.Break();
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
