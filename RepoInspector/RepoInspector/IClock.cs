using System;

namespace RepoInspector
{
    public interface IClock
    {
        public DateTime DateTimeNow();
        public DateTimeOffset DateTimeOffsetNow();
        
        public DateTime DateTimeUtcNow();
        public DateTimeOffset DateTimeOffsetUtcNow();
    }
}