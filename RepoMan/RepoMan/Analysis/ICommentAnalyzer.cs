using RepoMan.Repository;

namespace RepoMan.Analysis
{
    public interface ICommentAnalyzer
    {
        PullRequestCommentSnapshot CalculateCommentStatistics(PullRequestDetails prDetails);
    }
}