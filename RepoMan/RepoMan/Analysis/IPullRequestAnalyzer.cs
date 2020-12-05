using System.Collections.Immutable;
using RepoMan.Analysis.Scoring;
using RepoMan.Records;

namespace RepoMan.Analysis
{
    /// <summary>
    /// A pull request analyzer examines the characteristics of a single pull request. Scorers that operate on comments, approvals, etc. are expressed here.
    /// </summary>
    public interface IPullRequestAnalyzer
    {
        PullRequestMetrics CalculatePullRequestMetrics(PullRequest prDetails);
        IImmutableSet<Scorer> Scorers { get; }
    }
}