using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClockRuntime.Messaging;
using MemBus;
using Microsoft.Azure.Devices.Client;

namespace ClockRuntime.Services
{
    internal class IotHubService : IDisposable
    {
        private readonly DeviceClient _deviceClient;

        public IotHubService(SettingsProvider settings)
        {
            _deviceClient = DeviceClient.Create(settings.IoTHubHostname, AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(settings.IoTHubDeviceId, settings.IoTHubDeviceId), TransportType.Http1);
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
                        //todo
                        Debug.WriteLine(ex.Message);
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

        public void Dispose()
        {
            _deviceClient.Dispose();
        }
    }
}