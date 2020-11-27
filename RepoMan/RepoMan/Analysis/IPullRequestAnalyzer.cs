using RepoMan.Repository;

namespace RepoMan.Analysis
{
    /// <summary>
    /// A pull request analyzer examines the characteristics of a single pull request. Scorers that operate on comments, approvals, etc. are expressed here.
    /// </summary>
    public interface IPullRequestAnalyzer
    {
        PullRequestCommentSnapshot CalculateCommentStatistics(PullRequestDetails prDetails);
    }
}