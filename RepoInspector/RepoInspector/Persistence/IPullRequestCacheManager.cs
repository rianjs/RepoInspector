using System.Collections.Generic;
using System.Threading.Tasks;
using RepoInspector.Records;

namespace RepoInspector.Persistence
{
    /// <summary>
    /// A caching contract for pull requests that have been fully interrogated from their respective upstream systems of record. This is done because reading
    /// and writing a single JSON file is typically much faster than making multiple sequential calls to fully interrogate a single pull request, which is what
    /// most git APIs require, including GitHub and BitBucket.
    ///
    /// The most natural implementation for this contract is an object store like S3, or a filesystem location. Typically the object graph isn't directly queried
    /// so a document database is probably not the best approach.
    /// </summary>
    public interface IPullRequestCacheManager
    {
        /// <summary>
        /// Save the collection of fully-inspected pull requests. Typically this is as a single JSON file. If the JSON files get large, you could consider
        /// compressing them before writing them to their permanent home. Typically this file is overwritten as diffing the changes rarely has value.
        /// 
        /// A typical implementation will use the repoOwner and repoName as a sort of composite "primary key".
        /// 
        /// * A filesystem implementation might use a nested directory structure: repoOwner/resoName/pull-requests.json 
        /// * A relational database implementation might have repoOwner and repoName as column names with a third column representing the JSON blob
        /// * An object store might have a bucket named repoOwner-repoName, with pull-requests.json written in the bucket
        /// </summary>
        /// <param name="prDetails"></param>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <returns></returns>
        ValueTask SaveAsync(IList<PullRequest> prDetails, string repoOwner, string repoName);
        
        /// <summary>
        /// Returns the collection of previous-inspected pull requests, to short-circuit making calls to the git API for things that are already known to
        /// RepoInspector. 
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <returns></returns>
        ValueTask<IList<PullRequest>> LoadAsync(string repoOwner, string repoName);
    }
}