using System;

namespace RepoMan
{
    public interface IClock
    {
        public DateTime Now();
        public DateTime UtcNow();
    }
}