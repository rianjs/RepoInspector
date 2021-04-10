using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RepoInspector.Records;

namespace RepoInspector.Persistence
{
    /// <summary>
    /// The persistence contract for saving and loading metric snapshots. Backing stores might be a filesystem, S3, a document data store, etc.
    /// </summary>
    public interface IHistoricalAnalysisManager
    {
        /// <summary>
        /// Save the batch of metric snapshots.
        /// 
        /// When saving snapshots, implementations should use the UpdatedAt property of the record to render change over time. UpdatedAt represents the last
        /// time the a metric snapshot was touched. If analysis of a pull request was already accounted for, and the pull request is changed over the PR was
        /// first closed, RepoInspector will detect the change, remove it from the prior metric snapshot, and create (or update) the metric snapshot associated
        /// with the new UpdatedAt date. Pull requests are NOT double counted, even if they are touched after initial closure.
        ///
        /// A typical implementation will use the repoOwner and repoName as a sort of composite "primary key". The timestamp is typically the unique identifier
        /// for each metric snapshot.
        /// 
        /// * A filesystem implementation might use a nested directory structure, with each metric file called {timestamp}.json, and the known PRs as
        /// pull-requests.json 
        /// * A relational database implementation might have these as column names with a JSON column for the metric snapshots, and cached pull requests stashed
        /// in another table
        /// * An object store might have buckets named as repoOwner-repoName as a "parent directory" with each metric as {timestamp}.json and the known pull
        /// requests as pull-requests.json
        /// </summary>
        /// <param name="metricSnapshots"></param>
        /// <returns></returns>
        Task SaveAsync(List<MetricSnapshot> metricSnapshots);
        
        /// <summary>
        /// Loads a specific metric snapshots data set by timestamp.
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        ValueTask<MetricSnapshot> LoadAsync(string repoOwner, string repoName, DateTimeOffset timestamp);
        
        /// <summary>
        /// Loads all the historical metric snapshots for the repository. How you do this will be determined by the implementation of the persistence layer.
        /// 
        /// A typical implementation will use the repoOwner and repoName as a sort of composite "primary key". The timestamp is typically the unique identifier
        /// for each metric snapshot. 
        /// * A filesystem implementation might use a nested directory structure, with each metric file called {timestamp}.json, and the known PRs as
        /// pull-requests.json 
        /// * A relational database implementation might have these as column names with a JSON column for the metric snapshots, and cached pull requests stashed
        /// in another table
        /// * An object store might have buckets named as repoOwner-repoName as a "parent directory" with each metric as {timestamp}.json and the known pull
        /// requests as pull-requests.json
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <returns></returns>
        ValueTask<List<MetricSnapshot>> LoadHistoryAsync(string repoOwner, string repoName);
    }
}