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
        /// <param name="metricSnapshots"></param>
        /// <returns></returns>
        Task SaveAsync(List<MetricSnapshot> metricSnapshots);
        ValueTask<MetricSnapshot> LoadAsync(string repoOwner, string repoName, DateTimeOffset timestamp);
        ValueTask<List<MetricSnapshot>> LoadHistoryAsync(string repoOwner, string repoName);
    }
}