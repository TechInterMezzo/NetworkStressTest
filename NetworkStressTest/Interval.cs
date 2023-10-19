using System;
using System.Diagnostics;
using System.Threading;

namespace NetworkStressTest;

internal sealed class Interval
{
    private readonly Stopwatch _stopwatch;
    private readonly SpinWait _spinWait;
    private readonly TimeSpan _timeSpan;

    public Interval(TimeSpan timeSpan)
    {
        _stopwatch = Stopwatch.StartNew();
        _spinWait = new SpinWait();
        _timeSpan = timeSpan;
    }

    public bool IsElapsed()
    {
        if (_timeSpan > TimeSpan.Zero)
        {
            if (_stopwatch.Elapsed >= _timeSpan)
            {
                _stopwatch.Restart();
                return true;
            }
            return false;
        }
        return true;
    }

    public void WaitToElapse()
    {
        if (_timeSpan > TimeSpan.Zero)
        {
            _spinWait.Reset();
            while (!IsElapsed())
            {
                _spinWait.SpinOnce(-1);
            }
        }
    }
}
