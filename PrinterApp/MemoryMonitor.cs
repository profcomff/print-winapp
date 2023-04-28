using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using Serilog;

namespace PrinterApp;

public class MemoryMonitor
{
    private readonly DispatcherTimer _dispatcherTimer;
    private PerformanceCounter _systemAvailableMemoryCounter = null!;
    private PerformanceCounter _currentProcessUsingMemoryCounter = null!;
    private const int LastWarningSendDelaySec = 60 * 60;
    private const int LastStatusSendDelaySec = 60 * 60;
    private int _lastWarningSendSec;
    private int _lastStatusSendSec;

    public MemoryMonitor()
    {
        _dispatcherTimer = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 0, 0, 1, 0)
        };
        _dispatcherTimer.Tick += MemoryMonitorTick;
    }

    ~MemoryMonitor()
    {
        if (_systemAvailableMemoryCounter != null!)
            _systemAvailableMemoryCounter.Dispose();
        if (_currentProcessUsingMemoryCounter != null!)
            _currentProcessUsingMemoryCounter.Dispose();
    }

    public void StartTimer()
    {
        if (_dispatcherTimer.IsEnabled) return;
        new Task(() =>
        {
            _systemAvailableMemoryCounter =
                new PerformanceCounter("Memory", "Available MBytes");
            _currentProcessUsingMemoryCounter =
                new PerformanceCounter("Process", "Working Set",
                    Process.GetCurrentProcess().ProcessName);
            _dispatcherTimer.Start();
            Log.Information(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start timer");
        }).Start();
    }

    public void StopTimer()
    {
        Log.Information(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Stop timer");
        _dispatcherTimer.Stop();
    }

    private void MemoryMonitorTick(object? sender, EventArgs e)
    {
        var currentProcessUsingMemoryMBytes =
            _currentProcessUsingMemoryCounter.NextValue() / 1024f / 1024f;
        var systemAvailableMemoryMBytes = _systemAvailableMemoryCounter.NextValue();
        if (currentProcessUsingMemoryMBytes > 300 && _lastWarningSendSec == 0)
        {
            Marketing.MemoryStatusWarning("high memory consumption by the program",
                systemAvailableMemoryMBytes,
                currentProcessUsingMemoryMBytes);
            _lastWarningSendSec = LastWarningSendDelaySec;
        }
        else if (systemAvailableMemoryMBytes < 500 && _lastWarningSendSec == 0)
        {
            Marketing.MemoryStatusWarning("low system RAM", systemAvailableMemoryMBytes,
                currentProcessUsingMemoryMBytes);
            _lastWarningSendSec = LastWarningSendDelaySec;
        }

        if (_lastStatusSendSec == 0)
        {
            Marketing.MemoryStatus(systemAvailableMemoryMBytes, currentProcessUsingMemoryMBytes);
            _lastStatusSendSec = LastStatusSendDelaySec;
        }

        if (_lastStatusSendSec > 0)
        {
            _lastStatusSendSec--;
        }

        if (_lastWarningSendSec > 0)
        {
            _lastWarningSendSec--;
        }

        Log.Debug($"{currentProcessUsingMemoryMBytes} {systemAvailableMemoryMBytes}");
    }
}