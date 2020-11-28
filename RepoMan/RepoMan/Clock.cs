using System;

namespace RepoMan
{
    public class Clock :
        IClock
    {
        public DateTime DateTimeNow() => DateTime.Now;
        public DateTimeOffset DateTimeOffsetNow() => DateTimeOffset.Now;

        public DateTime DateTimeUtcNow() => DateTime.UtcNow;
        public DateTimeOffset DateTimeOffsetUtcNow() => DateTimeOffset.UtcNow;
    }
}