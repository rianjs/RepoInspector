using System;

namespace RepoMan
{
    public class Clock :
        IClock
    {
        public DateTime Now() => DateTime.Now;

        public DateTime UtcNow() => DateTime.UtcNow;
    }
}