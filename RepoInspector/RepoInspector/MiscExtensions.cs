using System;

namespace RepoInspector
{
    public static class MiscExtensions
    {
        private static readonly long _uSecDenominator = TimeSpan.TicksPerMillisecond / 1_000;
        public static long ToMicroseconds(this TimeSpan ts)
            => ts.Ticks / _uSecDenominator;
    }
}