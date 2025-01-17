// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public static class Time
{
    private const int SpinWaitIterations = 5;
    private const long YieldThreshold = 1000;
    private const long SleepThreshold = TimeSpan.TicksPerMillisecond;

    public static long CurrentTimeMillis => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

    public static long CurrentTimeMillisJava => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static bool WaitUntil(Func<bool> check, int maxWaitMilliseconds)
    {
        long ticksToWait = maxWaitMilliseconds * TimeSpan.TicksPerMillisecond;
        long start = DateTime.Now.Ticks;

        while (true)
        {
            long elapsed = DateTime.Now.Ticks - start;
            long ticksLeft = ticksToWait - elapsed;

            if (check())
            {
                return true;
            }

            if (ticksToWait <= 0)
            {
                return false;
            }

            if (elapsed >= ticksToWait)
            {
                return false;
            }

            DoWait(ticksLeft);

            if (check())
            {
                return true;
            }
        }
    }

    // Used by unit tests only
    public static void Wait(int maxWaitMilliseconds)
    {
        if (maxWaitMilliseconds <= 0)
        {
            return;
        }

        Thread.Sleep(maxWaitMilliseconds);
    }

    private static void DoWait(long ticksLeft)
    {
        if (ticksLeft > SleepThreshold)
        {
            Thread.Sleep(1);
        }
        else if (ticksLeft > YieldThreshold)
        {
            Thread.Yield();
        }
        else
        {
            Thread.SpinWait(SpinWaitIterations);
        }
    }
}
