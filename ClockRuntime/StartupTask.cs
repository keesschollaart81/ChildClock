using System;
using System.Threading;
using Windows.ApplicationModel.Background;
using ClockRuntime.Messaging;
using ClockRuntime.Services;
using Microsoft.IoT.Lightning.Providers;
using MemBus;
using MemBus.Configurators;
using Microsoft.Extensions.Logging;

namespace ClockRuntime
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static ILogger _logger;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            var loggerFactory = new LoggerFactory().AddDebug();
            _logger = loggerFactory.CreateLogger<StartupTask>();
            _logger.LogInformation("Starting BackgroundTask");

            if (!LightningProvider.IsLightningEnabled) return;

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            taskInstance.Canceled += (sender, reason) =>
            {
                _logger.LogDebug($"BackgroundTask Canceled, reason {reason}");
                tokenSource.Cancel();
            };

            try
            {
                using (var container = new IocContainer())
                {
                    var bus = BusSetup
                        .StartWith<Conservative>()
                        .Apply<IoCSupport>(s => s.SetAdapter(container).SetHandlerInterface(typeof(IHandleMessage<>)))
                        .Construct();

                    container.RegisterSingleton(bus);
                    container.RegisterSingleton(loggerFactory);

                    var iotHubService = container.GetInstance<IotHubService>();
                    await iotHubService.ReceiveAndPublishMessages(bus, token);
                }
                _logger.LogInformation("Services disposed gracefully!");
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Unhandled exception in backgroundtask, backgroundTask will now terminate", ex);
            }
            finally
            {
                deferral.Complete();
            }
            _logger.LogDebug("BackgroundTask finished");
        }
    }
}
