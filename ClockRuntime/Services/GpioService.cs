using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Pwm;
using Microsoft.IoT.Lightning.Providers;

namespace ClockRuntime.Services
{
    internal class GpioService : IDisposable
    {
        private readonly Dictionary<int, PwmPin> _pwmPins = new Dictionary<int, PwmPin>();

        public async Task SetCyclePercentage(int pinId, double cyclePercentage)
        {
            if (cyclePercentage > 1.0 || cyclePercentage < 0.0) throw new ArgumentOutOfRangeException(nameof(cyclePercentage));

            var controllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
            lock (this)
            {
                var pwmController = controllers[1];
                pwmController.SetDesiredFrequency(50);

                if (!_pwmPins.ContainsKey(pinId)) _pwmPins.Add(pinId, pwmController.OpenPin(pinId));

                var pin = _pwmPins[pinId];
                pin.SetActiveDutyCyclePercentage(cyclePercentage);
                pin.Start();
            }
        } 

        public void Dispose()
        {
            foreach (var pin in _pwmPins)
            {
                pin.Value.Stop();
            }
        }
    }
}