using System;
using System.Collections.Generic;

namespace RepoMan.Analysis
{
    public class RepositoryHealthAnalyzer
    {
        public RepositoryHealthSnapshot CalculateRepositoryHealthStatistics(IList<PullRequestCommentSnapshot> snapshots)
        {
            throw new NotImplementedException();
        }
        
        // In spite of these being comment- and PR-level measurements, they actually reflect over *repository* health. You can't tell anything about repo health
        // from one comment or PR in isolation. It's only in the aggregate that we can infer anything about health.
        // We could aggregate each of these measure lower down, but pre-aggregating would ruin the statistics, and computers are fast.
        
        // public int MedianCommentsPerPullRequest
        
        // public int MedianWordsPerComment
        
        // public double StdDeviationForPrCommentCount -- higher is better
        
        // public double StdDeviationForCommentWordCount -- higher is better
        
        // public TimeSpan MedianTimeToPrClosure
        
        // private int BusinessDaysToPrClosure -- 1-2 business days is best
    }
    
    public class RepositoryHealthSnapshot
    {
        public DateTimeOffset Timestamp { get; set; }
        public int PullRequestCount { get; set; }
        public int MedianCommentCountPerPullRequest { get; set; }
        public int MedianWordsPerComment { get; set; }
        public double PullRequestCommentCountStdDeviation { get; set; }
        public double CommentWordCountStdDeviation { get; set; }
        public int MedianSecondsToPullRequestClosure { get; set; }
        public double MedianBusinessDaysToPullRequestClosure { get; set; }
    }
}