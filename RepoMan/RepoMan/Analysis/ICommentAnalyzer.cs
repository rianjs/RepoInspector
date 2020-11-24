using System.Collections.Generic;
using RepoMan.PullRequest;

namespace RepoMan.Analysis
{
    public interface ICommentAnalyzer
    {
        PullRequestCommentSnapshot CalculateCommentStatistics(PullRequestDetails prDetails);

        /// <summary>
        /// Returns the list 
        /// </summary>
        /// <param name="prDetails"></param>
        /// <returns></returns>
        IDictionary<string, List<Comment>> GetApprovals(PullRequestDetails prDetails);
    }
}