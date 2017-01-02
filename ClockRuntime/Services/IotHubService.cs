using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClockRuntime.Messaging;
using MemBus;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging; 

namespace ClockRuntime.Services
{
    internal class IotHubService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly DeviceClient _deviceClient;

        public IotHubService(SettingsProvider settings, ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<IotHubService>();
            _deviceClient = DeviceClient.Create(settings.IoTHubHostname, AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(settings.IoTHubDeviceId, settings.IoTHubDeviceKey), TransportType.Http1);
        }

        public async Task ReceiveAndPublishMessages(IBus bus, CancellationToken token)
        {
            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested) break;

                    var message = await _deviceClient.ReceiveAsync(); //todo check with http?
                    if (message == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), token);
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
                        _logger.LogError("Unhandled exception while parsing message / publishing message to bus. Message will be ignored", ex);
                    }
                    finally
                    {
                        await _deviceClient.CompleteAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unhandled exception while receiving from IoT Hub, waiting for 5 seconds before retry...", ex);
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }

        public void Dispose()
        {
            _deviceClient.Dispose();
        }
    }
}