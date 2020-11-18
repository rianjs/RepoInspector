using System;
using System.Collections.Generic;

namespace RepoMan.Comments
{
    public class CommentAnalyzer
    {
        public PullRequestCommentStatistics CalculateStatistics()
        {
            throw new NotImplementedException();
        }
    }

    public class PullRequestCommentStatistics
    {
        public DateTimeOffset ComputedAt { get; set; }
        public int Count { get; set; }
        public int WordCount { get; set; }
    }
}