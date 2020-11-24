using System.Collections.Generic;

namespace RepoMan.Analysis
{
    public interface IRepositoryHealthAnalyzer
    {
        RepositoryHealthSnapshot CalculateRepositoryHealthStatistics(ICollection<PullRequestCommentSnapshot> snapshots);
    }
}