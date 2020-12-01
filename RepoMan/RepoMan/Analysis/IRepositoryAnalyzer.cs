using System.Collections.Generic;
using RepoMan.Records;

namespace RepoMan.Analysis
{
    /// <summary>
    /// A repository analyzer operates on a whole repository, by looking at the stream of pull request snapshots generated by a IPullRequestAnalyzer, and
    /// rolling up those outputs, along with any other repository-level statistics into a complete picture of a repository's health.  
    /// </summary>
    public interface IRepositoryAnalyzer
    {
        RepositoryMetrics CalculateRepositoryMetrics(ICollection<PullRequestMetrics> snapshots);
    }
}