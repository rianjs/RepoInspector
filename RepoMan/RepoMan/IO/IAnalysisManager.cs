using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RepoMan.Records;

namespace RepoMan.IO
{
    public interface IAnalysisManager
    {
        /// <summary>
        /// When saving snapshots, implementations should use the UpdatedAt property of the repoAnalysis record
        /// </summary>
        /// <param name="repoAnalysis"></param>
        /// <returns></returns>
        Task SaveAsync(RepositoryMetrics repoAnalysis);
        ValueTask<RepositoryMetrics> LoadAsync(string repoOwner, string repoName, DateTimeOffset timestamp);
        ValueTask<List<RepositoryMetrics>> LoadHistoryAsync(string repoOwner, string repoName);
    }
}