using System;
using System.Diagnostics;
using Windows.System.Threading;

namespace ClockRuntime.Services
{
    internal class AlarmService : IDisposable
    {
        private DateTime? _currentAlarmDateTime;
        private readonly ThreadPoolTimer _timer;
        private readonly GpioService _gpioService;

        public AlarmService(GpioService gpioService)
        {
            _timer = ThreadPoolTimer.CreatePeriodicTimer(TimerTick, TimeSpan.FromMilliseconds(3000));
            _gpioService = gpioService;
        }

        private void TimerTick(ThreadPoolTimer timer)
        {
            if (!_currentAlarmDateTime.HasValue) return;

            var minutesLeftToAlarm = (_currentAlarmDateTime.Value - DateTime.Now).TotalMinutes;

            double newCyclePercentage;
            if (minutesLeftToAlarm < 0) newCyclePercentage = 0.0;
            else if(minutesLeftToAlarm < -60) newCyclePercentage = minutesLeftToAlarm / 60;
            else if (minutesLeftToAlarm > 60) newCyclePercentage = 1.0;
            else newCyclePercentage = minutesLeftToAlarm / 60;

            _gpioService.SetCyclePercentage(5, newCyclePercentage).Wait();

            Debug.WriteLine($"Minutes left: {minutesLeftToAlarm}, Percentage: {newCyclePercentage}");
        }

        public void SetAlarmDateTime(DateTime dateTime)
        {
            _currentAlarmDateTime = dateTime;
        }

        public void Dispose()
        {
            _timer.Cancel();
        }
    }
}