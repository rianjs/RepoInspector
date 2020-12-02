using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RepoMan.Records;

namespace RepoMan.IO
{
    public interface IAnalysisManager
    {
        ValueTask SaveAsync(string repoOwner, string repoName, DateTime utcTimestamp, RepositoryMetrics repoAnalysis);
        ValueTask<RepositoryMetrics> LoadAsync(string repoOwner, string repoName, DateTime utcTimestamp);
        ValueTask<List<RepositoryMetrics>> LoadHistoryAsync(string repoOwner, string repoName);
    }
}