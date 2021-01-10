using System.Collections.Immutable;
using RepoInspector.Analysis.Scoring;
using RepoInspector.Records;

namespace RepoInspector.Analysis
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